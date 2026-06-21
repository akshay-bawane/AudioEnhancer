using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Executes external processes with bounded diagnostics and retry behavior.
/// </summary>
public sealed class ExternalProcessExecutor : IExternalProcessExecutor
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<ExternalProcessExecutor> _logger;

    public ExternalProcessExecutor(
        IProcessRunner processRunner,
        ILogger<ExternalProcessExecutor> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessRunResult> ExecuteAsync(
        ExternalProcessRequest request,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        int maxAttempts = Math.Max(1, request.RetryCount + 1);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeletePartialOutput(request.OutputPath);
            progress?.Report(attempt);

            try
            {
                _logger.LogInformation(
                    "Starting external process. Operation: {Operation}. Attempt: {Attempt}. MaxAttempts: {MaxAttempts}. FileName: {FileName}. Arguments: {Arguments}. WorkingDirectory: {WorkingDirectory}. Command: {Command}.",
                    request.OperationName,
                    attempt,
                    maxAttempts,
                    request.StartInfo.FileName,
                    string.Join(' ', request.StartInfo.ArgumentList),
                    ProcessCommandFormatter.GetWorkingDirectory(request.StartInfo),
                    ProcessCommandFormatter.FormatCommand(request.StartInfo));

                ProcessRunResult result = await _processRunner.RunAsync(request.StartInfo, cancellationToken);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation(
                        "External process completed. Operation: {Operation}. Attempt: {Attempt}. ExitCode: {ExitCode}.",
                        request.OperationName,
                        attempt,
                        result.ExitCode);

                    return result;
                }

                throw new InvalidOperationException(
                    $"{request.OperationName} failed with exit code {result.ExitCode}. Details: {GetBestError(result)}");
            }
            catch (OperationCanceledException)
            {
                DeletePartialOutput(request.OutputPath);
                _logger.LogWarning(
                    "External process was canceled. Operation: {Operation}. Attempt: {Attempt}.",
                    request.OperationName,
                    attempt);
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                DeletePartialOutput(request.OutputPath);

                _logger.LogWarning(
                    ex,
                    "External process failed and will be retried. Operation: {Operation}. Attempt: {Attempt}. MaxAttempts: {MaxAttempts}. RetryDelay: {RetryDelay}.",
                    request.OperationName,
                    attempt,
                    maxAttempts,
                    request.RetryDelay);

                if (request.RetryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(request.RetryDelay, cancellationToken);
                }
            }
            catch
            {
                DeletePartialOutput(request.OutputPath);
                throw;
            }
        }

        throw new InvalidOperationException(
            $"External process operation failed after {maxAttempts} attempts: {request.OperationName}.",
            lastException);
    }

    private void DeletePartialOutput(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        try
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                _logger.LogDebug("Deleted partial process output. OutputPath: {OutputPath}.", outputPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete partial process output. OutputPath: {OutputPath}.", outputPath);
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

        return "No process output was captured.";
    }
}
