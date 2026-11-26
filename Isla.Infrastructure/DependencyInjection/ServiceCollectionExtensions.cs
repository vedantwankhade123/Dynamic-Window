using Isla.Core.Interfaces;
using Isla.Core.Services;
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

        services.AddSingleton<MockDemoService>();
        services.AddSingleton<IMusicService>(sp => sp.GetRequiredService<MockDemoService>());
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<MockDemoService>());
        services.AddSingleton<ISystemMonitorService>(sp => sp.GetRequiredService<MockDemoService>());
        services.AddSingleton<IAiService>(sp => sp.GetRequiredService<MockDemoService>());
        services.AddSingleton<IDemoController>(sp => sp.GetRequiredService<MockDemoService>());

        services.AddSingleton<ISettingsService, SqliteSettingsService>();

        return services;
    }
}
