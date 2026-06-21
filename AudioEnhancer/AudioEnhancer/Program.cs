using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AudioEnhancer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddAudioEnhancerApplication(configuration)
            .BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

        IAudioEnhancementWorkflowService workflow = serviceProvider.GetRequiredService<IAudioEnhancementWorkflowService>();

        var result = await workflow.RunAsync();

        if (result.Approved && result.FinalVideoPath is not null)
        {
            System.Console.WriteLine($"Final enhanced video saved to: {result.FinalVideoPath}");
        }
        else
        {
            System.Console.WriteLine("No final video was generated.");
        }
    }
}
