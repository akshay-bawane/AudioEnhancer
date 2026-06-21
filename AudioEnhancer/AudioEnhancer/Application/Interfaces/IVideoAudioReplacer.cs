using AudioEnhancer.Application.Models;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

/// <summary>
/// Replaces the audio track in a video.
/// </summary>
public interface IVideoAudioReplacer
{
    /// <summary>
    /// Replaces the video's audio with the supplied enhanced audio file.
    /// </summary>
    /// <param name="request">The audio replacement request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <param name="progress">Receives operation progress updates.</param>
    /// <returns>The output video path.</returns>
    Task<string> ReplaceAudioAsync(
        VideoAudioReplacementRequest request,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null);
}
