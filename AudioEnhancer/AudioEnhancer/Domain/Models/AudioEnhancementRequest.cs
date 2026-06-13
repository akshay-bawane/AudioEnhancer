using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Domain.Models;

public sealed record AudioEnhancementRequest(
    string InputAudioPath,
    string OutputAudioPath,
    EnhancementProfile Profile);
