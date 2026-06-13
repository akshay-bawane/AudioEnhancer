using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

public interface IVideoAudioReplacer
{
    Task<string> ReplaceAudioAsync(VideoAudioReplacementRequest request, CancellationToken cancellationToken = default);
}
