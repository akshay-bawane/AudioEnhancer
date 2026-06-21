using AudioEnhancer.Application.Models;

namespace AudioEnhancer.Application.Interfaces;

/// <summary>
/// Provides FFmpeg-backed video and audio operations.
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Extracts the audio stream from a video into a WAV file.
    /// </summary>
    /// <param name="videoPath">The source video path.</param>
    /// <param name="outputAudioPath">The destination WAV path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <param name="progress">Receives operation progress updates.</param>
    /// <returns>The created WAV path.</returns>
    Task<string> ExtractAudioAsync(
        string videoPath,
        string outputAudioPath,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null);

    /// <summary>
    /// Replaces a video's audio stream with an enhanced audio file.
    /// </summary>
    /// <param name="originalVideo">The source video path.</param>
    /// <param name="enhancedAudio">The replacement audio path.</param>
    /// <param name="outputVideo">The destination video path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <param name="progress">Receives operation progress updates.</param>
    /// <returns>The created video path.</returns>
    Task<string> ReplaceAudioAsync(
        string originalVideo,
        string enhancedAudio,
        string outputVideo,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null);
}
