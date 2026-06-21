using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;
using Microsoft.Extensions.Logging;

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
    private readonly ITemporaryFileCleaner _temporaryFileCleaner;
    private readonly ILogger<AudioEnhancementWorkflow> _logger;

    public AudioEnhancementWorkflow(
        IAudioExtractor audioExtractor,
        IAudioEnhancerFactory audioEnhancerFactory,
        IAudioPreviewPlayer audioPreviewPlayer,
        IUserApprovalService userApprovalService,
        IVideoAudioReplacer videoAudioReplacer,
        IOutputPathService outputPathService,
        IConsoleInputService consoleInputService,
        ITemporaryFileCleaner temporaryFileCleaner,
        ILogger<AudioEnhancementWorkflow> logger)
    {
        _audioExtractor = audioExtractor;
        _audioEnhancerFactory = audioEnhancerFactory;
        _audioPreviewPlayer = audioPreviewPlayer;
        _userApprovalService = userApprovalService;
        _videoAudioReplacer = videoAudioReplacer;
        _outputPathService = outputPathService;
        _consoleInputService = consoleInputService;
        _temporaryFileCleaner = temporaryFileCleaner;
        _logger = logger;
    }

    public async Task<EnhancementWorkflowResult> RunAsync(EnhancementWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var temporaryFiles = new List<string>();
        string extractedAudioPath = _outputPathService.GetExtractedAudioPath(request.VideoPath);
        temporaryFiles.Add(extractedAudioPath);
        _logger.LogInformation(
            "Workflow extraction path generated. VideoPath: {VideoPath}. ExtractedWavPath: {ExtractedWavPath}.",
            Path.GetFullPath(request.VideoPath),
            Path.GetFullPath(extractedAudioPath));

        try
        {
            extractedAudioPath = await _audioExtractor.ExtractAsync(
                new AudioExtractionRequest(
                    request.VideoPath,
                    extractedAudioPath),
                cancellationToken);

            EnhancementProfile currentProfile = request.Profile;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string enhancedAudioPath = _outputPathService.GetEnhancedAudioPath(request.VideoPath, currentProfile);
                temporaryFiles.Add(enhancedAudioPath);
                IAudioEnhancementStrategy enhancementStrategy = _audioEnhancerFactory.GetStrategy(currentProfile);
                LogExistingFile("Enhancement input WAV still exists before enhancement", extractedAudioPath);
                _logger.LogInformation(
                    "Workflow enhancement paths. Profile: {Profile}. DeepFilterInputPath: {InputAudioPath}. EnhancedOutputPath: {EnhancedAudioPath}.",
                    currentProfile,
                    Path.GetFullPath(extractedAudioPath),
                    Path.GetFullPath(enhancedAudioPath));

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
                    _logger.LogInformation(
                        "Workflow final merge paths. VideoPath: {VideoPath}. EnhancedAudioPath: {EnhancedAudioPath}. FinalVideoPath: {FinalVideoPath}.",
                        Path.GetFullPath(request.VideoPath),
                        Path.GetFullPath(enhancementResult.EnhancedAudioPath),
                        Path.GetFullPath(finalVideoPath));

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
        finally
        {
            foreach (string temporaryFile in temporaryFiles)
            {
                _temporaryFileCleaner.DeleteIfExists(temporaryFile);
            }
        }
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
