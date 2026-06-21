namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Executes external process requests with retry, logging, and output cleanup.
/// </summary>
public interface IExternalProcessExecutor
{
    /// <summary>
    /// Executes a process request and returns the successful process result.
    /// </summary>
    /// <param name="request">The external process request.</param>
    /// <param name="progress">An optional attempt progress callback.</param>
    /// <param name="cancellationToken">A token that cancels execution.</param>
    /// <returns>The process result.</returns>
    Task<ProcessRunResult> ExecuteAsync(
        ExternalProcessRequest request,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
