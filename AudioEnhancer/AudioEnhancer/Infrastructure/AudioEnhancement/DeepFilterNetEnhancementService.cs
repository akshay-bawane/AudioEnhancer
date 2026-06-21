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
        string validatedInputWavFile = ValidateInputWav(inputWavFile);
        string validatedOutputWavFile = ValidateOutputPath(outputWavFile);
        EnsureExecutableConfigured();
        EnsureOutputDirectory(validatedOutputWavFile);

        _logger.LogInformation(
            "Starting DeepFilterNet enhancement. DeepFilterInputPath: {InputWavFile}. EnhancedOutputPath: {OutputWavFile}. DeepFilterExecutablePath: {ExecutablePath}.",
            validatedInputWavFile,
            validatedOutputWavFile,
            _options.DeepFilterNetPath);

        string[] arguments = new[] { validatedInputWavFile, "-o", validatedOutputWavFile };
        var startInfo = ProcessStartInfoFactory.Create(_options.DeepFilterNetPath, arguments);

        _logger.LogInformation(
            "DeepFilterNet command. ExecutablePath: {ExecutablePath}. Arguments: {Arguments}. WorkingDirectory: {WorkingDirectory}. Command: {Command}.",
            startInfo.FileName,
            string.Join(' ', startInfo.ArgumentList),
            ProcessCommandFormatter.GetWorkingDirectory(startInfo),
            ProcessCommandFormatter.FormatCommand(startInfo));

        ProcessRunResult result = await _processRunner.RunAsync(
            startInfo,
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

        ValidateOutputWavCreated(validatedOutputWavFile);

        _logger.LogInformation("DeepFilterNet enhancement completed: {OutputWavFile}.", validatedOutputWavFile);

        return validatedOutputWavFile;
    }

    private static string ValidateInputWav(string inputWavFile)
    {
        if (string.IsNullOrWhiteSpace(inputWavFile))
        {
            throw new ArgumentException("DeepFilterNet input WAV path is required.", nameof(inputWavFile));
        }

        string fullPath = GetFullPath(inputWavFile, nameof(inputWavFile));
        if (!string.Equals(Path.GetExtension(fullPath), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("DeepFilterNet input file must be a WAV file.", nameof(inputWavFile));
        }

        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("DeepFilterNet input WAV file was not found.", fullPath);
        }

        if (fileInfo.Length <= 0)
        {
            throw new InvalidOperationException($"DeepFilterNet input WAV file is empty: {fullPath}");
        }

        return fileInfo.FullName;
    }

    private static string ValidateOutputPath(string outputWavFile)
    {
        if (string.IsNullOrWhiteSpace(outputWavFile))
        {
            throw new ArgumentException("DeepFilterNet output WAV path is required.", nameof(outputWavFile));
        }

        string fullPath = GetFullPath(outputWavFile, nameof(outputWavFile));
        if (!string.Equals(Path.GetExtension(fullPath), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("DeepFilterNet output file must be a WAV file.", nameof(outputWavFile));
        }

        return fullPath;
    }

    private static void ValidateOutputWavCreated(string outputWavFile)
    {
        var fileInfo = new FileInfo(outputWavFile);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("DeepFilterNet output WAV file was not created.", outputWavFile);
        }

        if (fileInfo.Length <= 0)
        {
            throw new InvalidOperationException($"DeepFilterNet output WAV file is empty: {outputWavFile}");
        }
    }

    private void EnsureExecutableConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.DeepFilterNetPath))
        {
            throw new InvalidOperationException(
                "DeepFilterNet executable path is not configured. Set AudioEnhancer:DeepFilterNetPath in appsettings.json.");
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
