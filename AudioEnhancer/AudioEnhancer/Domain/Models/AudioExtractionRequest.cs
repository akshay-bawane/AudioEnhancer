namespace AudioEnhancer.Domain.Models;

public sealed record AudioExtractionRequest(
    string VideoPath,
    string OutputAudioPath);
