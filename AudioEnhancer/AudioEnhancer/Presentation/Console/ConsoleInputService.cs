using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Presentation.Console;

public sealed class ConsoleInputService : IConsoleInputService
{
    public Task<string> ReadVideoPathAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            System.Console.Write("Enter video path: ");
            string? videoPath = System.Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(videoPath))
            {
                return Task.FromResult(videoPath.Trim().Trim('"'));
            }

            System.Console.WriteLine("Video path cannot be empty.");
        }
    }

    public async Task<EnhancementProfile> ReadEnhancementProfileAsync(CancellationToken cancellationToken = default)
    {
        EnhancementProfile? profile = await ReadEnhancementProfileMenuAsync(cancellationToken);

        if (profile is null)
        {
            throw new OperationCanceledException("User selected Exit.", cancellationToken);
        }

        return profile.Value;
    }

    public Task<EnhancementProfile?> ReadEnhancementProfileMenuAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            System.Console.WriteLine();
            System.Console.WriteLine("Select enhancement profile:");
            System.Console.WriteLine("1. Fast Enhancement");
            System.Console.WriteLine("2. Balanced Enhancement");
            System.Console.WriteLine("3. Studio Enhancement");
            System.Console.WriteLine("4. Exit");
            System.Console.Write("Choice: ");

            string? choice = System.Console.ReadLine();

            switch (choice?.Trim())
            {
                case "1":
                    return Task.FromResult<EnhancementProfile?>(EnhancementProfile.Fast);
                case "2":
                    return Task.FromResult<EnhancementProfile?>(EnhancementProfile.Balanced);
                case "3":
                    return Task.FromResult<EnhancementProfile?>(EnhancementProfile.Studio);
                case "4":
                    return Task.FromResult<EnhancementProfile?>(null);
                default:
                    System.Console.WriteLine("Invalid selection. Choose 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    public Task<bool> ReadPlayPreviewAsync(CancellationToken cancellationToken = default)
    {
        return ReadYesNoAsync("Play preview? (Y/N): ", cancellationToken);
    }

    private static Task<bool> ReadYesNoAsync(string prompt, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            System.Console.Write(prompt);
            string? answer = System.Console.ReadLine();

            switch (answer?.Trim().ToUpperInvariant())
            {
                case "Y":
                case "YES":
                    return Task.FromResult(true);
                case "N":
                case "NO":
                    return Task.FromResult(false);
                default:
                    System.Console.WriteLine("Please enter Y or N.");
                    break;
            }
        }
    }
}
