using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.AudioExtraction;

public sealed class FfmpegAudioExtractor : IAudioExtractor
{
    private readonly IVideoService _videoService;

    public FfmpegAudioExtractor(IVideoService videoService)
    {
        _videoService = videoService;
    }

    public Task<string> ExtractAsync(AudioExtractionRequest request, CancellationToken cancellationToken = default)
    {
        return _videoService.ExtractAudioAsync(request.VideoPath);
    }
}
