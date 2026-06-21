using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Default implementation for running external processes.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessRunResult> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Process could not be started: {startInfo.FileName}.");
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            return new ProcessRunResult(process.ExitCode, standardOutput, standardError);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            _logger.LogWarning("Process was canceled and terminated. FileName: {FileName}.", startInfo.FileName);
            throw;
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Executable could not be started: {startInfo.FileName}.", ex);
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
}
