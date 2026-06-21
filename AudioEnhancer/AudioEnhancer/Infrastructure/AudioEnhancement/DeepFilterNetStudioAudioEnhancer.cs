using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class DeepFilterNetStudioAudioEnhancer : IAudioEnhancementStrategy
{
    private readonly IDeepFilterNetEnhancementService _deepFilterNetEnhancementService;
    private readonly ILogger<DeepFilterNetStudioAudioEnhancer> _logger;

    public DeepFilterNetStudioAudioEnhancer(
        IDeepFilterNetEnhancementService deepFilterNetEnhancementService,
        ILogger<DeepFilterNetStudioAudioEnhancer> logger)
    {
        _deepFilterNetEnhancementService = deepFilterNetEnhancementService;
        _logger = logger;
    }

    public EnhancementProfile Profile => EnhancementProfile.Studio;

    public async Task<AudioEnhancementResult> EnhanceAsync(AudioEnhancementRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Studio enhancement request. InputAudioPath: {InputAudioPath}. OutputAudioPath: {OutputAudioPath}. Profile: {Profile}.",
            Path.GetFullPath(request.InputAudioPath),
            Path.GetFullPath(request.OutputAudioPath),
            request.Profile);

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
