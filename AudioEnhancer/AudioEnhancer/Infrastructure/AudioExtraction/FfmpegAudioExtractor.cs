using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Models;
using AudioEnhancer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Infrastructure.AudioExtraction;

/// <summary>
/// Extracts audio from video files by delegating to the configured video service.
/// </summary>
public sealed class FfmpegAudioExtractor : IAudioExtractor
{
    private readonly IVideoService _videoService;
    private readonly ILogger<FfmpegAudioExtractor> _logger;

    public FfmpegAudioExtractor(
        IVideoService videoService,
        ILogger<FfmpegAudioExtractor> logger)
    {
        _videoService = videoService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ExtractAsync(
        AudioExtractionRequest request,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null)
    {
        string requestedOutputPath = Path.GetFullPath(request.OutputAudioPath);

        _logger.LogInformation(
            "Audio extraction request. VideoPath: {VideoPath}. RequestedOutputAudioPath: {RequestedOutputAudioPath}.",
            Path.GetFullPath(request.VideoPath),
            requestedOutputPath);

        string extractedAudioPath = await _videoService.ExtractAudioAsync(
            request.VideoPath,
            request.OutputAudioPath,
            cancellationToken,
            progress);

        string actualOutputPath = Path.GetFullPath(extractedAudioPath);
        if (!string.Equals(requestedOutputPath, actualOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Audio extraction returned a different WAV path than requested. Requested: '{requestedOutputPath}'. Actual: '{actualOutputPath}'.");
        }

        _logger.LogInformation(
            "Audio extraction path verified. ExtractedWavPath: {ExtractedWavPath}.",
            actualOutputPath);

        return actualOutputPath;
    }
}
