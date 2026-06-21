using System.Diagnostics;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Runs external processes and captures their output.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Starts a process and waits for it to exit.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <param name="cancellationToken">A token that cancels the running process.</param>
    /// <returns>The exit code, standard output, and standard error produced by the process.</returns>
    Task<ProcessRunResult> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default);
}
