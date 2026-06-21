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
    private readonly IExternalProcessExecutor _processExecutor;
    private readonly ILogger<FfmpegVideoService> _logger;

    public FfmpegVideoService(
        IOptions<AudioEnhancerOptions> options,
        IFfmpegInstallationValidator ffmpegInstallationValidator,
        IExternalProcessExecutor processExecutor,
        ILogger<FfmpegVideoService> logger)
    {
        _options = options.Value;
        _ffmpegInstallationValidator = ffmpegInstallationValidator;
        _processExecutor = processExecutor;
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

        await ExecuteFfmpegAsync(
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

        await ExecuteFfmpegAsync(
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

    private async Task ExecuteFfmpegAsync(
        string operation,
        string outputPath,
        IProgress<VideoProcessingProgress>? progress,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        var attemptProgress = new Progress<int>(attempt =>
            progress?.Report(new VideoProcessingProgress(operation, "FFmpeg started.", 0, attempt)));

        _logger.LogDebug(
            "Starting FFmpeg command. Operation: {Operation}. Command: {Command}.",
            operation,
            BuildCommandForLog(_options.FFmpegPath, arguments));

        ProcessRunResult result = await _processExecutor.ExecuteAsync(
            new ExternalProcessRequest(
                operation,
                ProcessStartInfoFactory.Create(_options.FFmpegPath, arguments),
                outputPath,
                _options.FFmpegRetryCount,
                TimeSpan.FromMilliseconds(Math.Max(0, _options.FFmpegRetryDelayMilliseconds))),
            attemptProgress,
            cancellationToken);

        LogProcessTail(operation, result);
        progress?.Report(new VideoProcessingProgress(operation, "FFmpeg completed.", 95));
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

    private void LogProcessTail(string operation, ProcessRunResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logger.LogDebug(
                "FFmpeg stdout tail. Operation: {Operation}. StandardOutput: {StandardOutput}.",
                operation,
                result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logger.LogDebug(
                "FFmpeg stderr tail. Operation: {Operation}. StandardError: {StandardError}.",
                operation,
                result.StandardError);
        }
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
