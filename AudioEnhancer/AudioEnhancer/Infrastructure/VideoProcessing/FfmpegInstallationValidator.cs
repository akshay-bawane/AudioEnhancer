using System.Diagnostics;
using AudioEnhancer.Infrastructure.Options;
using AudioEnhancer.Infrastructure.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.VideoProcessing;

/// <summary>
/// Validates the configured FFmpeg executable by running <c>ffmpeg -version</c>.
/// </summary>
public sealed class FfmpegInstallationValidator : IFfmpegInstallationValidator
{
    private readonly AudioEnhancerOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FfmpegInstallationValidator> _logger;

    public FfmpegInstallationValidator(
        IOptions<AudioEnhancerOptions> options,
        IProcessRunner processRunner,
        ILogger<FfmpegInstallationValidator> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FFmpegPath))
        {
            throw new InvalidOperationException(
                "FFmpeg executable path is not configured. Set AudioEnhancer:FFmpegPath in appsettings.json.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.FFmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-version");

        try
        {
            ProcessRunResult result = await _processRunner.RunAsync(startInfo, cancellationToken);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"FFmpeg validation failed with exit code {result.ExitCode}. Error: {GetBestError(result)}");
            }

            _logger.LogDebug("FFmpeg installation validated. Executable: {FFmpegPath}.", _options.FFmpegPath);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "FFmpeg executable could not be validated: {FFmpegPath}.", _options.FFmpegPath);
            throw new InvalidOperationException(
                $"FFmpeg executable could not be validated. Check AudioEnhancer:FFmpegPath in appsettings.json. Current value: '{_options.FFmpegPath}'.",
                ex);
        }
    }

    private static string GetBestError(ProcessRunResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError;
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return result.StandardOutput;
        }

        return "No FFmpeg output was captured.";
    }
}
