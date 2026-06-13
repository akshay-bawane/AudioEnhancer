using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Infrastructure.AudioEnhancement;

public sealed class AudioEnhancerFactory : IAudioEnhancerFactory
{
    private readonly IEnumerable<IAudioEnhancementStrategy> _strategies;

    public AudioEnhancerFactory(IEnumerable<IAudioEnhancementStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IAudioEnhancementStrategy GetStrategy(EnhancementProfile profile)
    {
        IAudioEnhancementStrategy? strategy = _strategies.FirstOrDefault(strategy => strategy.Profile == profile);

        return strategy ?? throw new InvalidOperationException($"No audio enhancement strategy is registered for profile '{profile}'.");
    }
}
