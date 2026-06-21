using System.Diagnostics;

namespace AudioEnhancer.Infrastructure.Processes;

/// <summary>
/// Formats process start information for diagnostics.
/// </summary>
public static class ProcessCommandFormatter
{
    /// <summary>
    /// Builds a command line string from a process start configuration.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>A diagnostic command string.</returns>
    public static string FormatCommand(ProcessStartInfo startInfo)
    {
        return $"{EscapeForLog(startInfo.FileName)} {string.Join(' ', startInfo.ArgumentList.Select(EscapeForLog))}".TrimEnd();
    }

    /// <summary>
    /// Returns the working directory that will be used by the process.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The configured or inherited working directory.</returns>
    public static string GetWorkingDirectory(ProcessStartInfo startInfo)
    {
        return string.IsNullOrWhiteSpace(startInfo.WorkingDirectory)
            ? Environment.CurrentDirectory
            : startInfo.WorkingDirectory;
    }

    private static string EscapeForLog(string argument)
    {
        return argument.Contains(' ', StringComparison.Ordinal)
            ? $"\"{argument}\""
            : argument;
    }
}
