using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class DeepFilterNetStudioAudioEnhancer : IAudioEnhancementStrategy
{
    private readonly IDeepFilterNetEnhancementService _deepFilterNetEnhancementService;

    public DeepFilterNetStudioAudioEnhancer(IDeepFilterNetEnhancementService deepFilterNetEnhancementService)
    {
        _deepFilterNetEnhancementService = deepFilterNetEnhancementService;
    }

    public EnhancementProfile Profile => EnhancementProfile.Studio;

    public async Task<AudioEnhancementResult> EnhanceAsync(AudioEnhancementRequest request, CancellationToken cancellationToken = default)
    {
        string enhancedAudioPath = await _deepFilterNetEnhancementService.EnhanceAsync(
            request.InputAudioPath,
            request.OutputAudioPath,
            cancellationToken);

        return new AudioEnhancementResult(
            request.InputAudioPath,
            enhancedAudioPath,
            request.Profile);
    }
}
