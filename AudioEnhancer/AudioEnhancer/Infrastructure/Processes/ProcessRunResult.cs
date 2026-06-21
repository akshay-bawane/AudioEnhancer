namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Captured output from an external process execution.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">Text captured from standard output.</param>
/// <param name="StandardError">Text captured from standard error.</param>
public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
