using Configuration.Sections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Configuration;

public static class BuilderServicesExtension
{
    public static void AddAzureAdConfigBinding(this IServiceCollection services)
    {
        services.AddOptions<AzureAd>().BindConfiguration("AzureAd").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureAd>>().Value);
    }

    public static void AddStorageConfigBinding(this IServiceCollection services)
    {
        services.AddOptions<Storage>().BindConfiguration("Storage").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Storage>>().Value);
    }

    public static void AddSmtpConfigBinding(this IServiceCollection services)
    {
        services.AddOptions<Smtp>().BindConfiguration("Smtp").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Smtp>>().Value);
    }

    public static void AddContainerRegistryConfigBinding(this IServiceCollection services)
    {
        services.AddOptions<ContainerRegistry>().BindConfiguration("ContainerRegistry").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ContainerRegistry>>().Value);
    }

    public static void AddSecurityConfigBinding(this IServiceCollection services)
    {
        services.AddOptions<Security>().BindConfiguration("Security").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Security>>().Value);
    }
}
