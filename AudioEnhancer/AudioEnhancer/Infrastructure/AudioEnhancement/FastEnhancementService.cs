using System.Diagnostics;
using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Infrastructure.Options;
using AudioEnhancer.Infrastructure.Processes;
using AudioEnhancer.Infrastructure.VideoProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class FastEnhancementService : IFastEnhancementService
{
    private const string FastEnhancementFilters = "highpass=f=80,lowpass=f=12000,afftdn,loudnorm";
    private readonly AudioEnhancerOptions _options;
    private readonly IFfmpegInstallationValidator _ffmpegInstallationValidator;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FastEnhancementService> _logger;

    public FastEnhancementService(
        IOptions<AudioEnhancerOptions> options,
        IFfmpegInstallationValidator ffmpegInstallationValidator,
        IProcessRunner processRunner,
        ILogger<FastEnhancementService> logger)
    {
        _options = options.Value;
        _ffmpegInstallationValidator = ffmpegInstallationValidator;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(inputWavFile, outputWavFile);
        EnsureOutputDirectory(outputWavFile);
        await _ffmpegInstallationValidator.ValidateAsync(cancellationToken);

        _logger.LogInformation(
            "Starting fast audio enhancement. Input: {InputWavFile}. Output: {OutputWavFile}.",
            inputWavFile,
            outputWavFile);

        await RunFfmpegWithRetryAsync(
            outputWavFile,
            cancellationToken,
            "-y",
            "-i", inputWavFile,
            "-af", FastEnhancementFilters,
            "-acodec", "pcm_s16le",
            outputWavFile);

        if (!File.Exists(outputWavFile))
        {
            throw new InvalidOperationException($"FFmpeg completed but did not create the expected enhanced file: {outputWavFile}");
        }

        _logger.LogInformation("Fast audio enhancement completed: {OutputWavFile}.", outputWavFile);

        return outputWavFile;
    }

    private async Task RunFfmpegWithRetryAsync(
        string outputWavFile,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        int maxAttempts = Math.Max(1, _options.FFmpegRetryCount + 1);
        TimeSpan retryDelay = TimeSpan.FromMilliseconds(Math.Max(0, _options.FFmpegRetryDelayMilliseconds));
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeletePartialOutput(outputWavFile);

            try
            {
                await RunFfmpegOnceAsync(cancellationToken, arguments);
                return;
            }
            catch (OperationCanceledException)
            {
                DeletePartialOutput(outputWavFile);
                _logger.LogWarning("FFmpeg fast enhancement was canceled.");
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                DeletePartialOutput(outputWavFile);

                _logger.LogWarning(
                    ex,
                    "FFmpeg fast enhancement failed and will be retried. Attempt: {Attempt}. MaxAttempts: {MaxAttempts}. RetryDelay: {RetryDelay}.",
                    attempt,
                    maxAttempts,
                    retryDelay);

                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
            catch
            {
                DeletePartialOutput(outputWavFile);
                throw;
            }
        }

        throw new InvalidOperationException("FFmpeg fast enhancement failed after all retry attempts.", lastException);
    }

    private async Task RunFfmpegOnceAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        ProcessStartInfo startInfo = CreateProcessStartInfo(arguments);

        _logger.LogDebug("Starting FFmpeg command: {Command}", BuildCommandForLog(_options.FFmpegPath, arguments));

        ProcessRunResult result = await _processRunner.RunAsync(startInfo, cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logger.LogDebug("FFmpeg stdout: {StandardOutput}", result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logger.LogDebug("FFmpeg stderr: {StandardError}", result.StandardError);
        }

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "FFmpeg fast enhancement failed with exit code {ExitCode}. Error: {StandardError}",
                result.ExitCode,
                result.StandardError);

            throw new InvalidOperationException(
                $"FFmpeg fast enhancement failed with exit code {result.ExitCode}. Details: {GetBestError(result)}");
        }
    }

    private static void ValidateInput(string inputWavFile, string outputWavFile)
    {
        if (string.IsNullOrWhiteSpace(inputWavFile))
        {
            throw new ArgumentException("Input WAV file path is required.", nameof(inputWavFile));
        }

        if (string.IsNullOrWhiteSpace(outputWavFile))
        {
            throw new ArgumentException("Output WAV file path is required.", nameof(outputWavFile));
        }

        if (!File.Exists(inputWavFile))
        {
            throw new FileNotFoundException("Input WAV file was not found.", inputWavFile);
        }

        string inputExtension = Path.GetExtension(inputWavFile);
        string outputExtension = Path.GetExtension(outputWavFile);

        if (!string.Equals(inputExtension, ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Input file must be a WAV file.", nameof(inputWavFile));
        }

        if (!string.Equals(outputExtension, ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Output file must be a WAV file.", nameof(outputWavFile));
        }
    }

    private static void EnsureOutputDirectory(string outputWavFile)
    {
        string? outputDirectory = Path.GetDirectoryName(outputWavFile);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
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

    private void DeletePartialOutput(string outputWavFile)
    {
        try
        {
            if (File.Exists(outputWavFile))
            {
                File.Delete(outputWavFile);
                _logger.LogDebug("Deleted partial fast enhancement output. OutputWavFile: {OutputWavFile}.", outputWavFile);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete partial fast enhancement output. OutputWavFile: {OutputWavFile}.", outputWavFile);
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
