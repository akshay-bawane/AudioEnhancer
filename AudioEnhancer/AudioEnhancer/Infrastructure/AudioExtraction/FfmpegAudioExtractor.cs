using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Models;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.AudioExtraction;

/// <summary>
/// Extracts audio from video files by delegating to the configured video service.
/// </summary>
public sealed class FfmpegAudioExtractor : IAudioExtractor
{
    private readonly IVideoService _videoService;

    public FfmpegAudioExtractor(IVideoService videoService)
    {
        _videoService = videoService;
    }

    /// <inheritdoc />
    public Task<string> ExtractAsync(
        AudioExtractionRequest request,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null)
    {
        return _videoService.ExtractAudioAsync(
            request.VideoPath,
            request.OutputAudioPath,
            cancellationToken,
            progress);
    }
}
