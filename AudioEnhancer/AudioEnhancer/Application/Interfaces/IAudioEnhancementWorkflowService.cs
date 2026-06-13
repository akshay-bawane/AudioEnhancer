using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Application.Interfaces;

public interface IAudioEnhancementWorkflowService
{
    Task<EnhancementWorkflowResult> RunAsync(CancellationToken cancellationToken = default);
}
