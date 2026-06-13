using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Domain.Models;

public sealed record AudioEnhancementResult(
    string OriginalAudioPath,
    string EnhancedAudioPath,
    EnhancementProfile Profile);
