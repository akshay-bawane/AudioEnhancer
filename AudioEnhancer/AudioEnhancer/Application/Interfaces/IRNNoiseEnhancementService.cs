namespace AudioEnhancer.Application.Interfaces;

public interface IRNNoiseEnhancementService
{
    Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default);
}
