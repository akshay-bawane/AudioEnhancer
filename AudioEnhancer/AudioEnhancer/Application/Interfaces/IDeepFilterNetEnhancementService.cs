namespace AudioEnhancer.Application.Interfaces;

public interface IDeepFilterNetEnhancementService
{
    Task<string> EnhanceAsync(
        string inputWavFile,
        string outputWavFile,
        CancellationToken cancellationToken = default);
}
