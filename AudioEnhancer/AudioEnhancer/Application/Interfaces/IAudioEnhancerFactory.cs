using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Application.Interfaces;

public interface IAudioEnhancerFactory
{
    IAudioEnhancementStrategy GetStrategy(EnhancementProfile profile);
}
