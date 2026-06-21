using System.Diagnostics;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Creates safe process start information for command-line tools.
/// </summary>
public static class ProcessStartInfoFactory
{
    /// <summary>
    /// Creates a redirected, non-shell process start configuration.
    /// </summary>
    /// <param name="fileName">The executable file name or path.</param>
    /// <param name="arguments">Arguments to pass as structured tokens.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/>.</returns>
    public static ProcessStartInfo Create(string fileName, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
