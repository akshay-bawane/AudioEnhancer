using AudioEnhancer.Domain.Enums;

namespace AudioEnhancer.Application.Interfaces;

public interface IConsoleInputService
{
    Task<string> ReadVideoPathAsync(CancellationToken cancellationToken = default);

    Task<EnhancementProfile> ReadEnhancementProfileAsync(CancellationToken cancellationToken = default);

    Task<EnhancementProfile?> ReadEnhancementProfileMenuAsync(CancellationToken cancellationToken = default);

    Task<bool> ReadPlayPreviewAsync(CancellationToken cancellationToken = default);
}
