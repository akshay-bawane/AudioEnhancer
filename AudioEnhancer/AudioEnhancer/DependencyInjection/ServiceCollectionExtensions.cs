using AudioEnhancer.Application.Interfaces;
using AudioEnhancer.Application.Workflows;
using AudioEnhancer.Infrastructure.AudioEnhancement;
using AudioEnhancer.Infrastructure.AudioExtraction;
using AudioEnhancer.Infrastructure.Options;
using AudioEnhancer.Infrastructure.Paths;
using AudioEnhancer.Infrastructure.Playback;
using AudioEnhancer.Infrastructure.Processes;
using AudioEnhancer.Infrastructure.VideoProcessing;
using AudioEnhancer.Presentation.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioEnhancer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAudioEnhancerApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AudioEnhancerOptions>(configuration.GetSection(AudioEnhancerOptions.SectionName));

        services.AddSingleton<IConsoleInputService, ConsoleInputService>();
        services.AddSingleton<IUserApprovalService, UserApprovalService>();
        services.AddSingleton<IOutputPathService, OutputPathService>();
        services.AddLogging(builder => builder.AddConsole());

        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IFfmpegInstallationValidator, FfmpegInstallationValidator>();
        services.AddSingleton<IVideoService, FfmpegVideoService>();
        services.AddSingleton<IAudioExtractor, FfmpegAudioExtractor>();
        services.AddSingleton<NAudioPreviewPlayer>();
        services.AddSingleton<IAudioPreviewService>(provider => provider.GetRequiredService<NAudioPreviewPlayer>());
        services.AddSingleton<IAudioPreviewPlayer>(provider => provider.GetRequiredService<NAudioPreviewPlayer>());
        services.AddSingleton<IVideoAudioReplacer, FfmpegVideoAudioReplacer>();

        services.AddSingleton<IFastEnhancementService, FastEnhancementService>();
        services.AddSingleton<IRNNoiseEnhancementService, RNNoiseEnhancementService>();
        services.AddSingleton<IDeepFilterNetEnhancementService, DeepFilterNetEnhancementService>();
        services.AddSingleton<IAudioEnhancementStrategy, FfmpegFastAudioEnhancer>();
        services.AddSingleton<IAudioEnhancementStrategy, RnNoiseBalancedAudioEnhancer>();
        services.AddSingleton<IAudioEnhancementStrategy, DeepFilterNetStudioAudioEnhancer>();
        services.AddSingleton<IAudioEnhancerFactory, AudioEnhancerFactory>();

        services.AddSingleton<IAudioEnhancementWorkflow, AudioEnhancementWorkflow>();
        services.AddSingleton<IAudioEnhancementWorkflowService, AudioEnhancementWorkflowService>();

        return services;
    }

    public static IServiceCollection AddAudioEnhancerApplication(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        return services.AddAudioEnhancerApplication(configuration);
    }
}
