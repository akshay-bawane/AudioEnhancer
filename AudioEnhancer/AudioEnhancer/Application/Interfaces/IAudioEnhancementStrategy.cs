using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

public interface IAudioEnhancementStrategy
{
    EnhancementProfile Profile { get; }

    Task<AudioEnhancementResult> EnhanceAsync(AudioEnhancementRequest request, CancellationToken cancellationToken = default);
}
