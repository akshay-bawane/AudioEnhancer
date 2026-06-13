using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class FfmpegFastAudioEnhancer : IAudioEnhancementStrategy
{
    private readonly IFastEnhancementService _fastEnhancementService;

    public FfmpegFastAudioEnhancer(IFastEnhancementService fastEnhancementService)
    {
        _fastEnhancementService = fastEnhancementService;
    }

    public EnhancementProfile Profile => EnhancementProfile.Fast;

    public async Task<AudioEnhancementResult> EnhanceAsync(AudioEnhancementRequest request, CancellationToken cancellationToken = default)
    {
        string enhancedAudioPath = await _fastEnhancementService.EnhanceAsync(
            request.InputAudioPath,
            request.OutputAudioPath,
            cancellationToken);

        return new AudioEnhancementResult(
            request.InputAudioPath,
            enhancedAudioPath,
            request.Profile);
    }
}
