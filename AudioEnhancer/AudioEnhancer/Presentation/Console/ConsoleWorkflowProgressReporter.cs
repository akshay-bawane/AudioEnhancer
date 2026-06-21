using AudioEnhancer.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Presentation.Console;

public sealed class ConsoleWorkflowProgressReporter : IWorkflowProgressReporter
{
    private readonly ILogger<ConsoleWorkflowProgressReporter> _logger;

    public ConsoleWorkflowProgressReporter(ILogger<ConsoleWorkflowProgressReporter> logger)
    {
        _logger = logger;
    }

    public void Report(string message)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"[AudioEnhancer] {message}");
        _logger.LogInformation("{Message}", message);
    }
}
