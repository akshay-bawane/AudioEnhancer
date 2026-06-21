using System.ComponentModel;
using System.Diagnostics;
using AudioEnhancer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Default implementation for running external processes.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly AudioEnhancerOptions _options;
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(
        IOptions<AudioEnhancerOptions> options,
        ILogger<ProcessRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessRunResult> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process { StartInfo = startInfo };
        var standardOutput = new BoundedLineBuffer(_options.ProcessOutputTailLines);
        var standardError = new BoundedLineBuffer(_options.ProcessOutputTailLines);

        try
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    standardOutput.Add(args.Data);
                    _logger.LogTrace(
                        "Process stdout. FileName: {FileName}. Line: {Line}.",
                        startInfo.FileName,
                        args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    standardError.Add(args.Data);
                    _logger.LogTrace(
                        "Process stderr. FileName: {FileName}. Line: {Line}.",
                        startInfo.FileName,
                        args.Data);
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException($"Process could not be started: {startInfo.FileName}.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit();

            return new ProcessRunResult(
                process.ExitCode,
                standardOutput.ToString(),
                standardError.ToString());
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            _logger.LogWarning("Process was canceled and terminated. FileName: {FileName}.", startInfo.FileName);
            throw;
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Executable could not be started: {startInfo.FileName}.", ex);
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private sealed class BoundedLineBuffer
    {
        private readonly Queue<string> _lines = new();
        private readonly object _syncRoot = new();
        private readonly int _maxLines;

        public BoundedLineBuffer(int maxLines)
        {
            _maxLines = Math.Max(1, maxLines);
        }

        public void Add(string line)
        {
            lock (_syncRoot)
            {
                _lines.Enqueue(line);

                while (_lines.Count > _maxLines)
                {
                    _lines.Dequeue();
                }
            }
        }

        public override string ToString()
        {
            lock (_syncRoot)
            {
                return string.Join(Environment.NewLine, _lines);
            }
        }
    }
}
