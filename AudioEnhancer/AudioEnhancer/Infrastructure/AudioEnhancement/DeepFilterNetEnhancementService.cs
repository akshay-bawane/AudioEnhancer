using System.Diagnostics;
using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class DeepFilterNetEnhancementService : IDeepFilterNetEnhancementService
{
    private readonly AudioEnhancerOptions _options;
    private readonly ILogger<DeepFilterNetEnhancementService> _logger;

    public DeepFilterNetEnhancementService(
        IOptions<AudioEnhancerOptions> options,
        ILogger<DeepFilterNetEnhancementService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(inputWavFile, outputWavFile);
        EnsureExecutableConfigured();
        EnsureOutputDirectory(outputWavFile);

        _logger.LogInformation(
            "Starting DeepFilterNet enhancement. Input: {InputWavFile}. Output: {OutputWavFile}.",
            inputWavFile,
            outputWavFile);

        ProcessResult result = await RunDeepFilterNetAsync(inputWavFile, outputWavFile, cancellationToken);

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "DeepFilterNet failed with exit code {ExitCode}. Stdout: {StandardOutput}. Stderr: {StandardError}.",
                result.ExitCode,
                result.StandardOutput,
                result.StandardError);

            throw new InvalidOperationException(
                $"DeepFilterNet failed with exit code {result.ExitCode}. Error: {GetBestError(result)}");
        }

        if (!File.Exists(outputWavFile))
        {
            throw new InvalidOperationException(
                $"DeepFilterNet completed successfully but did not create the expected enhanced WAV file: {outputWavFile}");
        }

        _logger.LogInformation("DeepFilterNet enhancement completed: {OutputWavFile}.", outputWavFile);

        return outputWavFile;
    }

    private async Task<ProcessResult> RunDeepFilterNetAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = CreateProcessStartInfo(inputWavFile, outputWavFile);

        try
        {
            _logger.LogDebug(
                "Starting DeepFilterNet command: {Command}",
                BuildCommandForLog(process.StartInfo.FileName, process.StartInfo.ArgumentList));

            if (!process.Start())
            {
                throw new InvalidOperationException("DeepFilterNet process could not be started.");
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            LogProcessOutput(standardOutput, standardError);

            return new ProcessResult(process.ExitCode, standardOutput, standardError);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            _logger.LogWarning("DeepFilterNet enhancement was canceled.");
            throw;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "DeepFilterNet executable could not be started: {ExecutablePath}.", _options.DeepFilterNetPath);
            throw new InvalidOperationException(
                $"DeepFilterNet executable could not be started. Check AudioEnhancer:DeepFilterNetPath in appsettings.json. Current value: '{_options.DeepFilterNetPath}'.",
                ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error while running DeepFilterNet enhancement.");
            throw new InvalidOperationException("Unexpected error while running DeepFilterNet enhancement.", ex);
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(string inputWavFile, string outputWavFile)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _options.DeepFilterNetPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        processStartInfo.ArgumentList.Add(inputWavFile);
        processStartInfo.ArgumentList.Add("-o");
        processStartInfo.ArgumentList.Add(outputWavFile);

        return processStartInfo;
    }

    private void EnsureExecutableConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.DeepFilterNetPath))
        {
            throw new InvalidOperationException(
                "DeepFilterNet executable path is not configured. Set AudioEnhancer:DeepFilterNetPath in appsettings.json.");
        }
    }

    private static void ValidateRequest(string inputWavFile, string outputWavFile)
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

        if (!string.Equals(Path.GetExtension(inputWavFile), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Input file must be a WAV file.", nameof(inputWavFile));
        }

        if (!string.Equals(Path.GetExtension(outputWavFile), ".wav", StringComparison.OrdinalIgnoreCase))
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

    private void LogProcessOutput(string standardOutput, string standardError)
    {
        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            _logger.LogInformation("DeepFilterNet stdout: {StandardOutput}", standardOutput);
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            _logger.LogInformation("DeepFilterNet stderr: {StandardError}", standardError);
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

    private static string GetBestError(ProcessResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError;
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return result.StandardOutput;
        }

        return "No DeepFilterNet output was captured.";
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

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
