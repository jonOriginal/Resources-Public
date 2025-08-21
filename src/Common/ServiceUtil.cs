using Microsoft.Extensions.DependencyInjection;

namespace Common;

public static class ServiceUtil
{
    public static void AddConfiguration<T>(this IServiceCollection services, T configuration) where T : class
    {
        services.AddSingleton(configuration);
    }
}