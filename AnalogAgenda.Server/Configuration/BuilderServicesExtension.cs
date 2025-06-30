using Configuration.Sections;
using Microsoft.Extensions.Options;

namespace AnalogAgenda.Server.Configuration;

public static class BuilderServicesExtension
{
    public static void AddConfigBindings(this IServiceCollection services)
    {
        services.AddOptions<AzureAd>().BindConfiguration("AzureAd").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureAd>>().Value);

        services.AddOptions<Storage>().BindConfiguration("Storage").ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Storage>>().Value);
    }
}
