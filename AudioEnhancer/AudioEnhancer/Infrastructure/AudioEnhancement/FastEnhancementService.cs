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
    private readonly IExternalProcessExecutor _processExecutor;
    private readonly ILogger<FastEnhancementService> _logger;

    public FastEnhancementService(
        IOptions<AudioEnhancerOptions> options,
        IFfmpegInstallationValidator ffmpegInstallationValidator,
        IExternalProcessExecutor processExecutor,
        ILogger<FastEnhancementService> logger)
    {
        _options = options.Value;
        _ffmpegInstallationValidator = ffmpegInstallationValidator;
        _processExecutor = processExecutor;
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

        await ExecuteFfmpegAsync(
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

    private async Task ExecuteFfmpegAsync(
        string outputWavFile,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        _logger.LogDebug("Starting FFmpeg command: {Command}", BuildCommandForLog(_options.FFmpegPath, arguments));

        ProcessRunResult result = await _processExecutor.ExecuteAsync(
            new ExternalProcessRequest(
                "FastAudioEnhancement",
                ProcessStartInfoFactory.Create(_options.FFmpegPath, arguments),
                outputWavFile,
                _options.FFmpegRetryCount,
                TimeSpan.FromMilliseconds(Math.Max(0, _options.FFmpegRetryDelayMilliseconds))),
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logger.LogDebug("FFmpeg stdout: {StandardOutput}", result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logger.LogDebug("FFmpeg stderr: {StandardError}", result.StandardError);
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
