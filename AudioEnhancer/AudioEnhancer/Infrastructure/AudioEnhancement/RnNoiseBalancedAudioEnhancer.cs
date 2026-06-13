using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class RnNoiseBalancedAudioEnhancer : IAudioEnhancementStrategy
{
    private readonly IRNNoiseEnhancementService _rnNoiseEnhancementService;

    public RnNoiseBalancedAudioEnhancer(IRNNoiseEnhancementService rnNoiseEnhancementService)
    {
        _rnNoiseEnhancementService = rnNoiseEnhancementService;
    }

    public EnhancementProfile Profile => EnhancementProfile.Balanced;

    public async Task<AudioEnhancementResult> EnhanceAsync(AudioEnhancementRequest request, CancellationToken cancellationToken = default)
    {
        string enhancedAudioPath = await _rnNoiseEnhancementService.EnhanceAsync(
            request.InputAudioPath,
            request.OutputAudioPath,
            cancellationToken);

        return new AudioEnhancementResult(
            request.InputAudioPath,
            enhancedAudioPath,
            request.Profile);
    }
}
