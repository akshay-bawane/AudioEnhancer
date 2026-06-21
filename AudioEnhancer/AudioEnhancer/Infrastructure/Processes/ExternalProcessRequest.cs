using System.Diagnostics;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Describes an external process operation that can be retried safely.
/// </summary>
/// <param name="OperationName">The logical operation name for logs and errors.</param>
/// <param name="StartInfo">The process start information.</param>
/// <param name="OutputPath">The expected output file that should be removed before retries or after failures.</param>
/// <param name="RetryCount">The number of retry attempts after the first attempt.</param>
/// <param name="RetryDelay">The delay between retry attempts.</param>
public sealed record ExternalProcessRequest(
    string OperationName,
    ProcessStartInfo StartInfo,
    string? OutputPath,
    int RetryCount,
    TimeSpan RetryDelay);
