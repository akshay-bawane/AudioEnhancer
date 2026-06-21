using AudioEnhancer.Application.Models;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

/// <summary>
/// Extracts audio from video files.
/// </summary>
public interface IAudioExtractor
{
    /// <summary>
    /// Extracts audio for the supplied request.
    /// </summary>
    /// <param name="request">The extraction request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <param name="progress">Receives operation progress updates.</param>
    /// <returns>The extracted audio path.</returns>
    Task<string> ExtractAsync(
        AudioExtractionRequest request,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null);
}
