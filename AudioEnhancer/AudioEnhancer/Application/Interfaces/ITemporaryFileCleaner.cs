namespace AudioEnhancer.Application.Interfaces;

/// <summary>
/// Deletes temporary workflow files.
/// </summary>
public interface ITemporaryFileCleaner
{
    /// <summary>
    /// Deletes a temporary file if it exists.
    /// </summary>
    /// <param name="filePath">The file to delete.</param>
    void DeleteIfExists(string? filePath);
}
