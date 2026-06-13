namespace AudioEnhancer.Application.Interfaces;

public interface IVideoService
{
    Task<string> ExtractAudioAsync(string videoPath);

    Task<string> ReplaceAudioAsync(
        string originalVideo,
        string enhancedAudio,
        string outputVideo);
}
