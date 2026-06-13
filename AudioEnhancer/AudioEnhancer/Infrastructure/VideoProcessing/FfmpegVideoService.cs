using System.Diagnostics;
using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.VideoProcessing;

public sealed class FfmpegVideoService : IVideoService
{
    private readonly AudioEnhancerOptions _options;
    private readonly ILogger<FfmpegVideoService> _logger;

    public FfmpegVideoService(
        IOptions<AudioEnhancerOptions> options,
        ILogger<FfmpegVideoService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExtractAudioAsync(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
        {
            throw new ArgumentException("Video path is required.", nameof(videoPath));
        }

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Input video file was not found.", videoPath);
        }

        string tempFolder = EnsureTempFolder(_options.TempFolder);
        string outputAudioPath = Path.Combine(
            tempFolder,
            $"{Path.GetFileNameWithoutExtension(videoPath)}_{Guid.NewGuid():N}.wav");

        _logger.LogInformation("Extracting audio from video {VideoPath} to {OutputAudioPath}.", videoPath, outputAudioPath);

        await RunFfmpegAsync(
            "-y",
            "-i", videoPath,
            "-vn",
            "-acodec", "pcm_s16le",
            "-ar", "48000",
            "-ac", "2",
            outputAudioPath);

        if (!File.Exists(outputAudioPath))
        {
            throw new InvalidOperationException($"FFmpeg completed but did not create the expected audio file: {outputAudioPath}");
        }

        _logger.LogInformation("Audio extraction completed: {OutputAudioPath}.", outputAudioPath);

        return outputAudioPath;
    }

    public async Task<string> ReplaceAudioAsync(
        string originalVideo,
        string enhancedAudio,
        string outputVideo)
    {
        if (string.IsNullOrWhiteSpace(originalVideo))
        {
            throw new ArgumentException("Original video path is required.", nameof(originalVideo));
        }

        if (string.IsNullOrWhiteSpace(enhancedAudio))
        {
            throw new ArgumentException("Enhanced audio path is required.", nameof(enhancedAudio));
        }

        if (string.IsNullOrWhiteSpace(outputVideo))
        {
            throw new ArgumentException("Output video path is required.", nameof(outputVideo));
        }

        if (!File.Exists(originalVideo))
        {
            throw new FileNotFoundException("Original video file was not found.", originalVideo);
        }

        if (!File.Exists(enhancedAudio))
        {
            throw new FileNotFoundException("Enhanced audio file was not found.", enhancedAudio);
        }

        string? outputDirectory = Path.GetDirectoryName(outputVideo);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        _logger.LogInformation(
            "Replacing audio in {OriginalVideo} with {EnhancedAudio}. Output: {OutputVideo}.",
            originalVideo,
            enhancedAudio,
            outputVideo);

        await RunFfmpegAsync(
            "-y",
            "-i", originalVideo,
            "-i", enhancedAudio,
            "-map", "0:v:0",
            "-map", "1:a:0",
            "-c:v", "copy",
            "-shortest",
            outputVideo);

        if (!File.Exists(outputVideo))
        {
            throw new InvalidOperationException($"FFmpeg completed but did not create the expected video file: {outputVideo}");
        }

        _logger.LogInformation("Audio replacement completed: {OutputVideo}.", outputVideo);

        return outputVideo;
    }

    private async Task RunFfmpegAsync(params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo = CreateProcessStartInfo(arguments);

        try
        {
            _logger.LogDebug("Starting FFmpeg command: {Command}", BuildCommandForLog(_options.FFmpegPath, arguments));

            if (!process.Start())
            {
                throw new InvalidOperationException("FFmpeg process could not be started.");
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                _logger.LogDebug("FFmpeg stdout: {StandardOutput}", standardOutput);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "FFmpeg failed with exit code {ExitCode}. Error: {StandardError}",
                    process.ExitCode,
                    standardError);

                throw new InvalidOperationException(
                    $"FFmpeg failed with exit code {process.ExitCode}. Details: {standardError}");
            }

            if (!string.IsNullOrWhiteSpace(standardError))
            {
                _logger.LogDebug("FFmpeg stderr: {StandardError}", standardError);
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error while running FFmpeg.");
            throw new InvalidOperationException("Unexpected error while running FFmpeg.", ex);
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

    private static string EnsureTempFolder(string configuredTempFolder)
    {
        string tempFolder = ResolveFolder(configuredTempFolder);
        Directory.CreateDirectory(tempFolder);

        return tempFolder;
    }

    private static string ResolveFolder(string folder)
    {
        return Path.IsPathRooted(folder)
            ? folder
            : Path.Combine(AppContext.BaseDirectory, folder);
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
