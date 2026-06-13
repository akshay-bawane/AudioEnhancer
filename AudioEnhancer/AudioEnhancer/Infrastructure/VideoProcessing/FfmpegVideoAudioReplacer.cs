using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.VideoProcessing;

public sealed class FfmpegVideoAudioReplacer : IVideoAudioReplacer
{
    private readonly IVideoService _videoService;

    public FfmpegVideoAudioReplacer(IVideoService videoService)
    {
        _videoService = videoService;
    }

    public Task<string> ReplaceAudioAsync(VideoAudioReplacementRequest request, CancellationToken cancellationToken = default)
    {
        return _videoService.ReplaceAudioAsync(
            request.OriginalVideoPath,
            request.EnhancedAudioPath,
            request.OutputVideoPath);
    }
}
