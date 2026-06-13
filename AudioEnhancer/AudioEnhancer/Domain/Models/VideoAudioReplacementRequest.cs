namespace AudioEnhancer.Domain.Models;

public sealed record VideoAudioReplacementRequest(
    string OriginalVideoPath,
    string EnhancedAudioPath,
    string OutputVideoPath);
