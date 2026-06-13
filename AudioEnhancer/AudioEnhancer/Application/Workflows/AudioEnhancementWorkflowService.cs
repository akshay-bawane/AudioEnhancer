using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Application.Workflows;

public sealed class AudioEnhancementWorkflowService : IAudioEnhancementWorkflowService
{
    private readonly IConsoleInputService _consoleInputService;
    private readonly IAudioExtractor _audioExtractor;
    private readonly IAudioEnhancerFactory _audioEnhancerFactory;
    private readonly IAudioPreviewService _audioPreviewService;
    private readonly IUserApprovalService _userApprovalService;
    private readonly IVideoAudioReplacer _videoAudioReplacer;
    private readonly IOutputPathService _outputPathService;
    private readonly ILogger<AudioEnhancementWorkflowService> _logger;

    public AudioEnhancementWorkflowService(
        IConsoleInputService consoleInputService,
        IAudioExtractor audioExtractor,
        IAudioEnhancerFactory audioEnhancerFactory,
        IAudioPreviewService audioPreviewService,
        IUserApprovalService userApprovalService,
        IVideoAudioReplacer videoAudioReplacer,
        IOutputPathService outputPathService,
        ILogger<AudioEnhancementWorkflowService> logger)
    {
        _consoleInputService = consoleInputService;
        _audioExtractor = audioExtractor;
        _audioEnhancerFactory = audioEnhancerFactory;
        _audioPreviewService = audioPreviewService;
        _userApprovalService = userApprovalService;
        _videoAudioReplacer = videoAudioReplacer;
        _outputPathService = outputPathService;
        _logger = logger;
    }

    public async Task<EnhancementWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
    {
        WriteProgress("AI Audio Enhancer");

        string videoPath = await _consoleInputService.ReadVideoPathAsync(cancellationToken);
        WriteProgress("Extracting audio from video...");

        string extractedAudioPath = await _audioExtractor.ExtractAsync(
            new AudioExtractionRequest(
                videoPath,
                _outputPathService.GetExtractedAudioPath(videoPath)),
            cancellationToken);

        WriteProgress($"Audio extracted: {extractedAudioPath}");

        while (true)
        {
            EnhancementProfile? selectedProfile = await _consoleInputService.ReadEnhancementProfileMenuAsync(cancellationToken);

            if (selectedProfile is null)
            {
                WriteProgress("Exiting without final video generation.");

                return new EnhancementWorkflowResult(
                    extractedAudioPath,
                    EnhancedAudioPath: string.Empty,
                    FinalVideoPath: null,
                    Approved: false);
            }

            EnhancementProfile profile = selectedProfile.Value;
            WriteProgress($"Enhancing audio with {profile} profile...");

            string enhancedAudioPath = _outputPathService.GetEnhancedAudioPath(videoPath, profile);
            IAudioEnhancementStrategy enhancementStrategy = _audioEnhancerFactory.GetStrategy(profile);

            AudioEnhancementResult enhancementResult = await enhancementStrategy.EnhanceAsync(
                new AudioEnhancementRequest(
                    extractedAudioPath,
                    enhancedAudioPath,
                    profile),
                cancellationToken);

            WriteProgress($"Enhanced audio saved: {enhancementResult.EnhancedAudioPath}");

            bool playPreview = await _consoleInputService.ReadPlayPreviewAsync(cancellationToken);
            if (playPreview)
            {
                WriteProgress("Playing enhanced audio preview...");
                await _audioPreviewService.PreviewAsync(enhancementResult.EnhancedAudioPath, cancellationToken);
            }

            bool approved = await _userApprovalService.RequestApprovalAsync(
                enhancementResult.EnhancedAudioPath,
                cancellationToken);

            if (!approved)
            {
                WriteProgress("Result rejected. Select another enhancement profile.");
                continue;
            }

            WriteProgress("Replacing original video audio...");

            string finalVideoPath = _outputPathService.GetFinalVideoPath(videoPath, profile);

            await _videoAudioReplacer.ReplaceAudioAsync(
                new VideoAudioReplacementRequest(
                    videoPath,
                    enhancementResult.EnhancedAudioPath,
                    finalVideoPath),
                cancellationToken);

            WriteProgress($"Final enhanced video saved: {finalVideoPath}");

            return new EnhancementWorkflowResult(
                extractedAudioPath,
                enhancementResult.EnhancedAudioPath,
                finalVideoPath,
                Approved: true);
        }
    }

    private void WriteProgress(string message)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"[AudioEnhancer] {message}");
        _logger.LogInformation("{Message}", message);
    }
}
