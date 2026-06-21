namespace AudioEnhancer.Application.Interfaces;

/// <summary>
/// Reports user-visible workflow progress without coupling application services to a UI.
/// </summary>
public interface IWorkflowProgressReporter
{
    /// <summary>
    /// Reports a progress message.
    /// </summary>
    /// <param name="message">The progress message.</param>
    void Report(string message);
}
