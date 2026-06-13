using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Application.Interfaces;

public interface IOutputPathService
{
    string GetExtractedAudioPath(string videoPath);

    string GetEnhancedAudioPath(string videoPath, EnhancementProfile profile);

    string GetFinalVideoPath(string videoPath, EnhancementProfile profile);
}
