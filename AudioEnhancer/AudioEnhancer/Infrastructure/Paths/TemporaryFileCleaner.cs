using AudioEnhancer.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.Infrastructure.Paths;

public sealed class TemporaryFileCleaner : ITemporaryFileCleaner
{
    private readonly ILogger<TemporaryFileCleaner> _logger;

    public TemporaryFileCleaner(ILogger<TemporaryFileCleaner> logger)
    {
        _logger = logger;
    }

    public void DeleteIfExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted temporary file. FilePath: {FilePath}.", filePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not delete temporary file. FilePath: {FilePath}.", filePath);
        }
    }
}
