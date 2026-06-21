using System.Diagnostics;
using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Models;
using AudioEnhancer.Infrastructure.Options;
using AudioEnhancer.Infrastructure.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.VideoProcessing;

/// <summary>
/// Provides FFmpeg-backed video operations with validation, retry, progress, and cleanup.
/// </summary>
public sealed class FfmpegVideoService : IVideoService
{
    private readonly AudioEnhancerOptions _options;
    private readonly IFfmpegInstallationValidator _ffmpegInstallationValidator;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FfmpegVideoService> _logger;

    public FfmpegVideoService(
        IOptions<AudioEnhancerOptions> options,
        IFfmpegInstallationValidator ffmpegInstallationValidator,
        IProcessRunner processRunner,
        ILogger<FfmpegVideoService> logger)
    {
        _options = options.Value;
        _ffmpegInstallationValidator = ffmpegInstallationValidator;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ExtractAudioAsync(
        string videoPath,
        string outputAudioPath,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string validatedVideoPath = ValidateExistingFile(videoPath, nameof(videoPath), "Input video file");
        string validatedOutputAudioPath = ValidateOutputPath(outputAudioPath, nameof(outputAudioPath), ".wav");
        EnsureNotSamePath(validatedVideoPath, validatedOutputAudioPath, nameof(outputAudioPath));
        EnsureOutputDirectory(validatedOutputAudioPath);

        await _ffmpegInstallationValidator.ValidateAsync(cancellationToken);

        _logger.LogInformation(
            "Extracting audio from video. VideoPath: {VideoPath}. OutputAudioPath: {OutputAudioPath}.",
            validatedVideoPath,
            validatedOutputAudioPath);

        await RunFfmpegWithRetryAsync(
            "ExtractAudio",
            validatedOutputAudioPath,
            progress,
            cancellationToken,
            "-y",
            "-i", validatedVideoPath,
            "-vn",
            "-acodec", "pcm_s16le",
            "-ar", "48000",
            "-ac", "2",
            validatedOutputAudioPath);

        EnsureCreated(validatedOutputAudioPath, "audio file");
        progress?.Report(new VideoProcessingProgress("ExtractAudio", "Audio extraction completed.", 100));

        _logger.LogInformation("Audio extraction completed. OutputAudioPath: {OutputAudioPath}.", validatedOutputAudioPath);

        return validatedOutputAudioPath;
    }

    /// <inheritdoc />
    public async Task<string> ReplaceAudioAsync(
        string originalVideo,
        string enhancedAudio,
        string outputVideo,
        CancellationToken cancellationToken = default,
        IProgress<VideoProcessingProgress>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string validatedOriginalVideo = ValidateExistingFile(originalVideo, nameof(originalVideo), "Original video file");
        string validatedEnhancedAudio = ValidateExistingFile(enhancedAudio, nameof(enhancedAudio), "Enhanced audio file");
        string validatedOutputVideo = ValidateOutputPath(outputVideo, nameof(outputVideo));
        EnsureNotSamePath(validatedOriginalVideo, validatedOutputVideo, nameof(outputVideo));
        EnsureNotSamePath(validatedEnhancedAudio, validatedOutputVideo, nameof(outputVideo));
        EnsureOutputDirectory(validatedOutputVideo);

        await _ffmpegInstallationValidator.ValidateAsync(cancellationToken);

        _logger.LogInformation(
            "Replacing video audio. OriginalVideo: {OriginalVideo}. EnhancedAudio: {EnhancedAudio}. OutputVideo: {OutputVideo}.",
            validatedOriginalVideo,
            validatedEnhancedAudio,
            validatedOutputVideo);

        await RunFfmpegWithRetryAsync(
            "ReplaceAudio",
            validatedOutputVideo,
            progress,
            cancellationToken,
            "-y",
            "-i", validatedOriginalVideo,
            "-i", validatedEnhancedAudio,
            "-map", "0:v:0",
            "-map", "1:a:0",
            "-c:v", "copy",
            "-shortest",
            validatedOutputVideo);

        EnsureCreated(validatedOutputVideo, "video file");
        progress?.Report(new VideoProcessingProgress("ReplaceAudio", "Audio replacement completed.", 100));

        _logger.LogInformation("Audio replacement completed. OutputVideo: {OutputVideo}.", validatedOutputVideo);

        return validatedOutputVideo;
    }

    private async Task RunFfmpegWithRetryAsync(
        string operation,
        string outputPath,
        IProgress<VideoProcessingProgress>? progress,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        int maxAttempts = Math.Max(1, _options.FFmpegRetryCount + 1);
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(Math.Max(0, _options.FFmpegRetryDelayMilliseconds));
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeletePartialOutput(outputPath);

            progress?.Report(new VideoProcessingProgress(operation, "FFmpeg started.", 0, attempt));

            try
            {
                await RunFfmpegOnceAsync(operation, cancellationToken, arguments);
                progress?.Report(new VideoProcessingProgress(operation, "FFmpeg completed.", 95, attempt));
                return;
            }
            catch (OperationCanceledException)
            {
                DeletePartialOutput(outputPath);
                _logger.LogWarning(
                    "FFmpeg operation was canceled. Operation: {Operation}. Attempt: {Attempt}. OutputPath: {OutputPath}.",
                    operation,
                    attempt,
                    outputPath);
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                DeletePartialOutput(outputPath);

                _logger.LogWarning(
                    ex,
                    "FFmpeg operation failed and will be retried. Operation: {Operation}. Attempt: {Attempt}. MaxAttempts: {MaxAttempts}. RetryDelay: {RetryDelay}.",
                    operation,
                    attempt,
                    maxAttempts,
                    retryDelay);

                progress?.Report(new VideoProcessingProgress(operation, "FFmpeg failed; retrying.", null, attempt));

                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
            catch
            {
                DeletePartialOutput(outputPath);
                throw;
            }
        }

        throw new InvalidOperationException($"FFmpeg operation failed after {maxAttempts} attempts: {operation}.", lastException);
    }

    private async Task RunFfmpegOnceAsync(
        string operation,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        ProcessStartInfo startInfo = CreateProcessStartInfo(arguments);

        _logger.LogDebug(
            "Starting FFmpeg command. Operation: {Operation}. Command: {Command}.",
            operation,
            BuildCommandForLog(_options.FFmpegPath, arguments));

        ProcessRunResult result = await _processRunner.RunAsync(startInfo, cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logger.LogDebug(
                "FFmpeg stdout. Operation: {Operation}. StandardOutput: {StandardOutput}.",
                operation,
                result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logger.LogDebug(
                "FFmpeg stderr. Operation: {Operation}. StandardError: {StandardError}.",
                operation,
                result.StandardError);
        }

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "FFmpeg failed. Operation: {Operation}. ExitCode: {ExitCode}. Error: {StandardError}.",
                operation,
                result.ExitCode,
                result.StandardError);

            throw new InvalidOperationException(
                $"FFmpeg failed during {operation} with exit code {result.ExitCode}. Details: {GetBestError(result)}");
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(IReadOnlyList<string> arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _options.FFmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        return processStartInfo;
    }

    private static string ValidateExistingFile(string path, string parameterName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException($"{displayName} path is required.", parameterName);
        }

        string fullPath = GetFullPath(path, parameterName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"{displayName} was not found.", fullPath);
        }

        return fullPath;
    }

    private static string ValidateOutputPath(
        string path,
        string parameterName,
        string? requiredExtension = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path is required.", parameterName);
        }

        string fullPath = GetFullPath(path, parameterName);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Output path must include a directory or resolvable file location.", parameterName);
        }

        if (!string.IsNullOrWhiteSpace(requiredExtension) &&
            !string.Equals(Path.GetExtension(fullPath), requiredExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Output path must use the {requiredExtension} extension.", parameterName);
        }

        return fullPath;
    }

    private static string GetFullPath(string path, string parameterName)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException("Path is not valid.", parameterName, ex);
        }
    }

    private static void EnsureNotSamePath(string inputPath, string outputPath, string parameterName)
    {
        if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Output path must be different from input paths.", parameterName);
        }
    }

    private static void EnsureOutputDirectory(string outputPath)
    {
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
    }

    private static void EnsureCreated(string outputPath, string fileDescription)
    {
        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException(
                $"FFmpeg completed but did not create the expected {fileDescription}: {outputPath}");
        }
    }

    private void DeletePartialOutput(string outputPath)
    {
        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                _logger.LogDebug("Deleted partial FFmpeg output. OutputPath: {OutputPath}.", outputPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete partial FFmpeg output. OutputPath: {OutputPath}.", outputPath);
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

    private static string BuildCommandForLog(string executablePath, IEnumerable<string> arguments)
    {
        return $"{EscapeForLog(executablePath)} {string.Join(' ', arguments.Select(EscapeForLog))}";
    }

    private static string EscapeForLog(string argument)
    {
        return argument.Contains(' ', StringComparison.Ordinal)
            ? $"\"{argument}\""
            : argument;
    }
}
