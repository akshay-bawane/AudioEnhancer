using System.Diagnostics;
using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class FastEnhancementService : IFastEnhancementService
{
    private const string FastEnhancementFilters = "highpass=f=80,lowpass=f=12000,afftdn,loudnorm";
    private readonly AudioEnhancerOptions _options;
    private readonly ILogger<FastEnhancementService> _logger;

    public FastEnhancementService(
        IOptions<AudioEnhancerOptions> options,
        ILogger<FastEnhancementService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(inputWavFile, outputWavFile);
        EnsureOutputDirectory(outputWavFile);

        _logger.LogInformation(
            "Starting fast audio enhancement. Input: {InputWavFile}. Output: {OutputWavFile}.",
            inputWavFile,
            outputWavFile);

        await RunFfmpegAsync(
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

    private async Task RunFfmpegAsync(CancellationToken cancellationToken, params string[] arguments)
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

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                _logger.LogDebug("FFmpeg stdout: {StandardOutput}", standardOutput);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "FFmpeg fast enhancement failed with exit code {ExitCode}. Error: {StandardError}",
                    process.ExitCode,
                    standardError);

                throw new InvalidOperationException(
                    $"FFmpeg fast enhancement failed with exit code {process.ExitCode}. Details: {standardError}");
            }

            if (!string.IsNullOrWhiteSpace(standardError))
            {
                _logger.LogDebug("FFmpeg stderr: {StandardError}", standardError);
            }
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            _logger.LogWarning("FFmpeg fast enhancement was canceled.");
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error while running FFmpeg fast enhancement.");
            throw new InvalidOperationException("Unexpected error while running FFmpeg fast enhancement.", ex);
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

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
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
