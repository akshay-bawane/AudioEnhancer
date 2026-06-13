namespace AudioEnhancer.Application.Interfaces;

public interface IAudioPreviewPlayer
{
    Task PlayAsync(string audioPath, CancellationToken cancellationToken = default);
}
