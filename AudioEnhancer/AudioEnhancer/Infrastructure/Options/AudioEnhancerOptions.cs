namespace AudioEnhancer.Infrastructure.Options;

public sealed class AudioEnhancerOptions
{
    public const string SectionName = "AudioEnhancer";

    public string FFmpegPath { get; init; } = "ffmpeg";

    public string DeepFilterNetPath { get; init; } = "deep-filter";

    public string RNNoisePath { get; init; } = "rnnoise";

    public string TempFolder { get; init; } = "Temp";

    public string OutputFolder { get; init; } = "Output";
}
