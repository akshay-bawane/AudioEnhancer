namespace AudioEnhancer.Infrastructure.Options;

public sealed class AudioEnhancerOptions
{
    public const string SectionName = "AudioEnhancer";

    public string FFmpegPath { get; init; } = "ffmpeg";

    public string DeepFilterNetPath { get; init; } = "deep-filter";

    public string RNNoisePath { get; init; } = "rnnoise";

    public string TempFolder { get; init; } = "Temp";

    public string OutputFolder { get; init; } = "Output";

    public int FFmpegRetryCount { get; init; } = 2;

    public int FFmpegRetryDelayMilliseconds { get; init; } = 500;

    public int ProcessOutputTailLines { get; init; } = 200;
}
