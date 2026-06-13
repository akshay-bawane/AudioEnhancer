using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

public interface IAudioEnhancementWorkflow
{
    Task<EnhancementWorkflowResult> RunAsync(EnhancementWorkflowRequest request, CancellationToken cancellationToken = default);
}
