using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;
using AudioEnhancer.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AudioEnhancer.Infrastructure.Paths;

public sealed class OutputPathService : IOutputPathService
{
    private readonly AudioEnhancerOptions _options;

    public OutputPathService(IOptions<AudioEnhancerOptions> options)
    {
        _options = options.Value;
    }

    public string GetExtractedAudioPath(string videoPath)
    {
        string tempFolder = EnsureFolder(_options.TempFolder);
        string fileName = $"{Path.GetFileNameWithoutExtension(videoPath)}_extracted_{Guid.NewGuid():N}.wav";

        return Path.Combine(tempFolder, fileName);
    }

    public string GetEnhancedAudioPath(string videoPath, EnhancementProfile profile)
    {
        string tempFolder = EnsureFolder(_options.TempFolder);
        string fileName = $"{Path.GetFileNameWithoutExtension(videoPath)}_{profile.ToString().ToLowerInvariant()}_enhanced_{Guid.NewGuid():N}.wav";

        return Path.Combine(tempFolder, fileName);
    }

    public string GetFinalVideoPath(string videoPath, EnhancementProfile profile)
    {
        string directory = EnsureFolder(_options.OutputFolder);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoPath);
        string extension = Path.GetExtension(videoPath);
        string fileName = $"{fileNameWithoutExtension}_{profile.ToString().ToLowerInvariant()}_enhanced{extension}";

        return Path.Combine(directory, fileName);
    }

    private static string EnsureFolder(string configuredFolder)
    {
        string folder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(AppContext.BaseDirectory, configuredFolder);

        Directory.CreateDirectory(folder);

        return folder;
    }
}
