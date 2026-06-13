namespace AudioEnhancer.Application.Interfaces;

public interface IUserApprovalService
{
    Task<bool> RequestApprovalAsync(string enhancedAudioPath, CancellationToken cancellationToken = default);
}
