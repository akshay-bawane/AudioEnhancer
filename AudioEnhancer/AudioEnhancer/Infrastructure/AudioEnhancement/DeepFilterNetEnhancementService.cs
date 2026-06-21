using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Infrastructure.Options;
using AudioEnhancer.Infrastructure.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class DeepFilterNetEnhancementService : IDeepFilterNetEnhancementService
{
    private readonly AudioEnhancerOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<DeepFilterNetEnhancementService> _logger;

    public DeepFilterNetEnhancementService(
        IOptions<AudioEnhancerOptions> options,
        IProcessRunner processRunner,
        ILogger<DeepFilterNetEnhancementService> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
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

        ProcessRunResult result = await _processRunner.RunAsync(
            ProcessStartInfoFactory.Create(
                _options.DeepFilterNetPath,
                new[] { inputWavFile, "-o", outputWavFile }),
            cancellationToken);

        LogProcessOutput(result);

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "DeepFilterNet failed with exit code {ExitCode}. StdoutTail: {StandardOutput}. StderrTail: {StandardError}.",
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

    private void LogProcessOutput(ProcessRunResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logger.LogDebug("DeepFilterNet stdout tail: {StandardOutput}", result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logger.LogDebug("DeepFilterNet stderr tail: {StandardError}", result.StandardError);
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

        return "No DeepFilterNet output was captured.";
    }
}
