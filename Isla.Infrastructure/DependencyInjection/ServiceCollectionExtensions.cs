using Isla.Core.Interfaces;
using Isla.Core.Services;
using Isla.Infrastructure.AI;
using Isla.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Isla.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIslaCoreServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<ActivityManager>();
        services.AddSingleton<IActivityService>(sp => sp.GetRequiredService<ActivityManager>());

        services.AddSingleton<RealTimerAndNotificationService>();
        services.AddSingleton<ITimerService>(sp => sp.GetRequiredService<RealTimerAndNotificationService>());
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<RealTimerAndNotificationService>());

        services.AddSingleton<ISettingsService, SqliteSettingsService>();
        services.AddSingleton<IAiService, LocalAiService>();

        return services;
    }
}
