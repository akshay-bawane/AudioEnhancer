namespace AudioEnhancer.Application.Models;

/// <summary>
/// Reports progress for video and audio processing operations.
/// </summary>
/// <param name="Operation">The operation currently being performed.</param>
/// <param name="Message">A human-readable progress message.</param>
/// <param name="PercentComplete">The approximate completion percentage when it is known.</param>
/// <param name="Attempt">The current retry attempt for the operation.</param>
public sealed record VideoProcessingProgress(
    string Operation,
    string Message,
    double? PercentComplete = null,
    int Attempt = 1);
