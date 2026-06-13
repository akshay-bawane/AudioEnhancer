namespace AudioEnhancer.Application.Interfaces;

public interface IFastEnhancementService
{
    Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default);
}
