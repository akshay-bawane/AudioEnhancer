using AudioEnhancer.Application.Interfaces;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace AudioEnhancer.Infrastructure.Playback;

public sealed class NAudioPreviewPlayer : IAudioPreviewPlayer, IAudioPreviewService
{
    private readonly ILogger<NAudioPreviewPlayer> _logger;
    private readonly object _syncRoot = new();
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioReader;
    private string? _currentAudioPath;
    private bool _disposed;

    public NAudioPreviewPlayer(ILogger<NAudioPreviewPlayer> logger)
    {
        _logger = logger;
    }

    public async Task PlayAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateAudioPath(audioPath);

        TaskCompletionSource playbackCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_syncRoot)
        {
            LoadAudioFile(audioPath);

            if (_waveOut is null)
            {
                throw new InvalidOperationException("Audio output device was not initialized.");
            }

            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Play();
        }

        _logger.LogInformation("Playing audio preview: {AudioPath}.", audioPath);

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() =>
        {
            Stop();
            playbackCompleted.TrySetCanceled(cancellationToken);
        });

        await playbackCompleted.Task;

        void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {
            if (sender is WaveOutEvent waveOut)
            {
                waveOut.PlaybackStopped -= OnPlaybackStopped;
            }

            if (args.Exception is not null)
            {
                _logger.LogError(args.Exception, "Audio playback failed for {AudioPath}.", audioPath);
                playbackCompleted.TrySetException(args.Exception);
                return;
            }

            _logger.LogInformation("Audio playback stopped: {AudioPath}.", audioPath);
            playbackCompleted.TrySetResult();
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            if (_waveOut?.PlaybackState != PlaybackState.Playing)
            {
                return;
            }

            _waveOut.Pause();
        }

        _logger.LogInformation("Audio preview paused.");
    }

    public void Stop()
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            if (_waveOut is null)
            {
                return;
            }

            _waveOut.Stop();

            if (_audioReader is not null)
            {
                _audioReader.Position = 0;
            }
        }

        _logger.LogInformation("Audio preview stopped.");
    }

    public TimeSpan GetDuration(string audioPath)
    {
        ThrowIfDisposed();
        ValidateAudioPath(audioPath);

        using var reader = new AudioFileReader(audioPath);
        return reader.TotalTime;
    }

    public async Task PreviewAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateAudioPath(audioPath);

        TimeSpan duration = GetDuration(audioPath);
        System.Console.WriteLine($"Preview duration: {duration:hh\\:mm\\:ss}");

        bool replay;
        do
        {
            replay = false;
            using CancellationTokenSource playbackCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task playbackTask = PlayAsync(audioPath, playbackCancellation.Token);

            System.Console.WriteLine("Controls: P = pause/resume, S = stop, R = replay, Q = finish preview");

            while (!playbackTask.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!System.Console.KeyAvailable)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                ConsoleKey key = System.Console.ReadKey(intercept: true).Key;

                switch (key)
                {
                    case ConsoleKey.P:
                        TogglePause();
                        break;
                    case ConsoleKey.S:
                        Stop();
                        break;
                    case ConsoleKey.R:
                        replay = true;
                        playbackCancellation.Cancel();
                        break;
                    case ConsoleKey.Q:
                        playbackCancellation.Cancel();
                        replay = false;
                        break;
                }
            }

            await IgnoreCancellationAsync(playbackTask);

            if (!replay && AskReplay())
            {
                replay = true;
            }
        }
        while (replay && !cancellationToken.IsCancellationRequested);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioReader?.Dispose();

            _waveOut = null;
            _audioReader = null;
            _currentAudioPath = null;
            _disposed = true;
        }
    }

    private void TogglePause()
    {
        lock (_syncRoot)
        {
            if (_waveOut is null)
            {
                return;
            }

            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
                _logger.LogInformation("Audio preview paused.");
                return;
            }

            if (_waveOut.PlaybackState == PlaybackState.Paused)
            {
                _waveOut.Play();
                _logger.LogInformation("Audio preview resumed.");
            }
        }
    }

    private void LoadAudioFile(string audioPath)
    {
        if (string.Equals(_currentAudioPath, audioPath, StringComparison.OrdinalIgnoreCase) && _audioReader is not null && _waveOut is not null)
        {
            _audioReader.Position = 0;
            return;
        }

        _waveOut?.Stop();
        _waveOut?.Dispose();
        _audioReader?.Dispose();

        _audioReader = new AudioFileReader(audioPath);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_audioReader);
        _currentAudioPath = audioPath;
    }

    private static bool AskReplay()
    {
        System.Console.Write("Replay preview? (y/n): ");
        string? answer = System.Console.ReadLine();

        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void ValidateAudioPath(string audioPath)
    {
        if (string.IsNullOrWhiteSpace(audioPath))
        {
            throw new ArgumentException("Audio path is required.", nameof(audioPath));
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("Audio file was not found.", audioPath);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
