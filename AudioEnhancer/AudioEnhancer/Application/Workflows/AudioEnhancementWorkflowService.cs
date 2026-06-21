using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Models;
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
    private readonly ITemporaryFileCleaner _temporaryFileCleaner;
    private readonly IWorkflowProgressReporter _progressReporter;
    private readonly ILogger<AudioEnhancementWorkflowService> _logger;

    public AudioEnhancementWorkflowService(
        IConsoleInputService consoleInputService,
        IAudioExtractor audioExtractor,
        IAudioEnhancerFactory audioEnhancerFactory,
        IAudioPreviewService audioPreviewService,
        IUserApprovalService userApprovalService,
        IVideoAudioReplacer videoAudioReplacer,
        IOutputPathService outputPathService,
        ITemporaryFileCleaner temporaryFileCleaner,
        IWorkflowProgressReporter progressReporter,
        ILogger<AudioEnhancementWorkflowService> logger)
    {
        _consoleInputService = consoleInputService;
        _audioExtractor = audioExtractor;
        _audioEnhancerFactory = audioEnhancerFactory;
        _audioPreviewService = audioPreviewService;
        _userApprovalService = userApprovalService;
        _videoAudioReplacer = videoAudioReplacer;
        _outputPathService = outputPathService;
        _temporaryFileCleaner = temporaryFileCleaner;
        _progressReporter = progressReporter;
        _logger = logger;
    }

    public async Task<EnhancementWorkflowResult> RunAsync(CancellationToken cancellationToken = default)
    {
        WriteProgress("AI Audio Enhancer");
        var videoProgress = new Progress<VideoProcessingProgress>(ReportVideoProgress);
        var temporaryFiles = new List<string>();

        string videoPath = await _consoleInputService.ReadVideoPathAsync(cancellationToken);
        _logger.LogInformation("Workflow video input selected. VideoPath: {VideoPath}.", Path.GetFullPath(videoPath));
        WriteProgress("Extracting audio from video...");

        string extractedAudioPath = _outputPathService.GetExtractedAudioPath(videoPath);
        temporaryFiles.Add(extractedAudioPath);
        _logger.LogInformation(
            "Workflow extraction path generated. VideoPath: {VideoPath}. ExtractedWavPath: {ExtractedWavPath}.",
            Path.GetFullPath(videoPath),
            Path.GetFullPath(extractedAudioPath));

        try
        {
            extractedAudioPath = await _audioExtractor.ExtractAsync(
                new AudioExtractionRequest(
                    videoPath,
                    extractedAudioPath),
                cancellationToken,
                videoProgress);

            WriteProgress($"Audio extracted: {extractedAudioPath}");
            LogExistingFile("Extracted WAV ready for enhancement", extractedAudioPath);

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
                temporaryFiles.Add(enhancedAudioPath);
                IAudioEnhancementStrategy enhancementStrategy = _audioEnhancerFactory.GetStrategy(profile);
                _logger.LogInformation(
                    "Workflow enhancement paths. Profile: {Profile}. DeepFilterInputPath: {InputAudioPath}. EnhancedOutputPath: {EnhancedAudioPath}.",
                    profile,
                    Path.GetFullPath(extractedAudioPath),
                    Path.GetFullPath(enhancedAudioPath));
                LogExistingFile("Enhancement input WAV still exists before enhancement", extractedAudioPath);

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
                _logger.LogInformation(
                    "Workflow final merge paths. VideoPath: {VideoPath}. EnhancedAudioPath: {EnhancedAudioPath}. FinalVideoPath: {FinalVideoPath}.",
                    Path.GetFullPath(videoPath),
                    Path.GetFullPath(enhancementResult.EnhancedAudioPath),
                    Path.GetFullPath(finalVideoPath));

                await _videoAudioReplacer.ReplaceAudioAsync(
                    new VideoAudioReplacementRequest(
                        videoPath,
                        enhancementResult.EnhancedAudioPath,
                        finalVideoPath),
                    cancellationToken,
                    videoProgress);

                WriteProgress($"Final enhanced video saved: {finalVideoPath}");

                return new EnhancementWorkflowResult(
                    extractedAudioPath,
                    enhancementResult.EnhancedAudioPath,
                    finalVideoPath,
                    Approved: true);
            }
        }
        finally
        {
            foreach (string temporaryFile in temporaryFiles)
            {
                _temporaryFileCleaner.DeleteIfExists(temporaryFile);
            }
        }
    }

    private void WriteProgress(string message)
    {
        _progressReporter.Report(message);
    }

    private void ReportVideoProgress(VideoProcessingProgress progress)
    {
        string percentage = progress.PercentComplete is { } value
            ? $" ({value:0.#}%)"
            : string.Empty;

        WriteProgress($"{progress.Operation}: {progress.Message}{percentage}");
    }

    private void LogExistingFile(string message, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        _logger.LogInformation(
            "{Message}. FilePath: {FilePath}. Exists: {Exists}. Length: {Length}.",
            message,
            fileInfo.FullName,
            fileInfo.Exists,
            fileInfo.Exists ? fileInfo.Length : 0);
    }
}
