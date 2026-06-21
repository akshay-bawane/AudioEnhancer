using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Models;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.VideoProcessing;

/// <summary>
/// Replaces video audio by delegating to the configured FFmpeg video service.
/// </summary>
public sealed class FfmpegVideoAudioReplacer : IVideoAudioReplacer
{
    private readonly IVideoService _videoService;

    public FfmpegVideoAudioReplacer(IVideoService videoService)
    {
        _videoService = videoService;
    }

    /// <inheritdoc />
    public Task<string> ReplaceAudioAsync(
        VideoAudioReplacementRequest request,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null)
    {
        return _videoService.ReplaceAudioAsync(
            request.OriginalVideoPath,
            request.EnhancedAudioPath,
            request.OutputVideoPath,
            cancellationToken,
            progress);
    }
}
