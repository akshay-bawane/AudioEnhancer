using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Workflows;

public sealed class AudioEnhancementWorkflow : IAudioEnhancementWorkflow
{
    private readonly IAudioExtractor _audioExtractor;
    private readonly IAudioEnhancerFactory _audioEnhancerFactory;
    private readonly IAudioPreviewPlayer _audioPreviewPlayer;
    private readonly IUserApprovalService _userApprovalService;
    private readonly IVideoAudioReplacer _videoAudioReplacer;
    private readonly IOutputPathService _outputPathService;
    private readonly IConsoleInputService _consoleInputService;

    public AudioEnhancementWorkflow(
        IAudioExtractor audioExtractor,
        IAudioEnhancerFactory audioEnhancerFactory,
        IAudioPreviewPlayer audioPreviewPlayer,
        IUserApprovalService userApprovalService,
        IVideoAudioReplacer videoAudioReplacer,
        IOutputPathService outputPathService,
        IConsoleInputService consoleInputService)
    {
        _audioExtractor = audioExtractor;
        _audioEnhancerFactory = audioEnhancerFactory;
        _audioPreviewPlayer = audioPreviewPlayer;
        _userApprovalService = userApprovalService;
        _videoAudioReplacer = videoAudioReplacer;
        _outputPathService = outputPathService;
        _consoleInputService = consoleInputService;
    }

    public async Task<EnhancementWorkflowResult> RunAsync(EnhancementWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        string extractedAudioPath = await _audioExtractor.ExtractAsync(
            new AudioExtractionRequest(
                request.VideoPath,
                _outputPathService.GetExtractedAudioPath(request.VideoPath)),
            cancellationToken);

        EnhancementProfile currentProfile = request.Profile;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string enhancedAudioPath = _outputPathService.GetEnhancedAudioPath(request.VideoPath, currentProfile);
            IAudioEnhancementStrategy enhancementStrategy = _audioEnhancerFactory.GetStrategy(currentProfile);

            AudioEnhancementResult enhancementResult = await enhancementStrategy.EnhanceAsync(
                new AudioEnhancementRequest(
                    extractedAudioPath,
                    enhancedAudioPath,
                    currentProfile),
                cancellationToken);

            bool playPreview = await _consoleInputService.ReadPlayPreviewAsync(cancellationToken);
            if (playPreview)
            {
                await _audioPreviewPlayer.PlayAsync(enhancementResult.EnhancedAudioPath, cancellationToken);
            }

            bool approved = await _userApprovalService.RequestApprovalAsync(
                enhancementResult.EnhancedAudioPath,
                cancellationToken);

            if (approved)
            {
                string finalVideoPath = _outputPathService.GetFinalVideoPath(request.VideoPath, currentProfile);

                await _videoAudioReplacer.ReplaceAudioAsync(
                    new VideoAudioReplacementRequest(
                        request.VideoPath,
                        enhancementResult.EnhancedAudioPath,
                        finalVideoPath),
                    cancellationToken);

                return new EnhancementWorkflowResult(
                    extractedAudioPath,
                    enhancementResult.EnhancedAudioPath,
                    finalVideoPath,
                    Approved: true);
            }

            EnhancementProfile? nextProfile = await _consoleInputService.ReadEnhancementProfileMenuAsync(cancellationToken);
            if (nextProfile is null)
            {
                return new EnhancementWorkflowResult(
                    extractedAudioPath,
                    enhancementResult.EnhancedAudioPath,
                    FinalVideoPath: null,
                    Approved: false);
            }

            currentProfile = nextProfile.Value;
        }
    }
}
