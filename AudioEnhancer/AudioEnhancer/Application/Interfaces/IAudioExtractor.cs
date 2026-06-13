using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

public interface IAudioExtractor
{
    Task<string> ExtractAsync(AudioExtractionRequest request, CancellationToken cancellationToken = default);
}
