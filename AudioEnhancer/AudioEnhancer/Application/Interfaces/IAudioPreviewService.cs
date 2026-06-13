namespace AudioEnhancer.Application.Interfaces;

public interface IAudioPreviewService : IDisposable
{
    Task PlayAsync(string audioPath, CancellationToken cancellationToken = default);

    void Pause();

    void Stop();

    TimeSpan GetDuration(string audioPath);

    Task PreviewAsync(string audioPath, CancellationToken cancellationToken = default);
}
