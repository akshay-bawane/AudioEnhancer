namespace AudioEnhancer.Domain.Models;

public sealed record EnhancementWorkflowResult(
    string ExtractedAudioPath,
    string EnhancedAudioPath,
    string? FinalVideoPath,
    bool Approved);
