namespace AudioEnhancer.Infrastructure.VideoProcessing;

/// <summary>
/// Validates that FFmpeg is configured and can be executed.
/// </summary>
public interface IFfmpegInstallationValidator
{
    /// <summary>
    /// Ensures the configured FFmpeg executable is available.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels validation.</param>
    Task ValidateAsync(CancellationToken cancellationToken = default);
}
