using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Domain.Models;

public sealed record EnhancementWorkflowRequest(
    string VideoPath,
    EnhancementProfile Profile);
