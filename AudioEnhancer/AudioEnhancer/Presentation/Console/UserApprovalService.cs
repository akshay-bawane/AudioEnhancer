using AudioEnhancer.Application.Interfaces;

namespace AudioEnhancer.Presentation.Console;

public sealed class UserApprovalService : IUserApprovalService
{
    public Task<bool> RequestApprovalAsync(string enhancedAudioPath, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"Enhanced audio: {enhancedAudioPath}");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            System.Console.Write("Approve result? (Y/N): ");
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
