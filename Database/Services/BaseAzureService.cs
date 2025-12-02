using Azure.Core;
using Configuration.Sections;
using Database.Helpers;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Database.Services;

/// <summary>
/// Base class for Azure services that provides common caching and client creation patterns
/// </summary>
/// <typeparam name="TClient">The type of Azure client to create and cache</typeparam>
public abstract class BaseAzureService<TClient>(AzureAd azureAdCfg, Storage storageCfg, Configuration.Sections.System systemCfg, IConfiguration configuration, string serviceEndpoint) where TClient : class
{
    // Build connection string from Storage config parts
    public static string? BuildConnectionStringFromParts(Storage storageCfg)
    {
        if (string.IsNullOrEmpty(storageCfg.DefaultEndpointsProtocol) || 
            string.IsNullOrEmpty(storageCfg.AccountKey) || 
            string.IsNullOrEmpty(storageCfg.BlobEndpoint))
        {
            return null;
        }
        
        var result = $"DefaultEndpointsProtocol={storageCfg.DefaultEndpointsProtocol};AccountName={storageCfg.AccountName};AccountKey={storageCfg.AccountKey};BlobEndpoint={storageCfg.BlobEndpoint}";
        
        if (!string.IsNullOrEmpty(storageCfg.EndpointSuffix))
        {
            result += $";EndpointSuffix={storageCfg.EndpointSuffix}";
        }
        
        return result + ";";
    }
    
    // Construct account URI based on IsDev flag
    private static Uri ConstructAccountUri(Storage storageCfg, Configuration.Sections.System systemCfg, string serviceEndpoint)
    {
        if (systemCfg.IsDev && !string.IsNullOrEmpty(storageCfg.BlobEndpoint))
        {
            // Development: Use BlobEndpoint from connection string (Azurite)
            return new Uri(storageCfg.BlobEndpoint);
        }
        
        // Production: Construct Azure Storage URL
        return new Uri($"https://{storageCfg.AccountName}.{serviceEndpoint}.core.windows.net");
    }
    
    // Get connection string from ConnectionStrings section (Aspire) or build from Storage config parts
    private static string? GetConnectionString(Storage storageCfg, IConfiguration configuration)
    {
        // First try to get from ConnectionStrings section (used by Aspire)
        var aspireConnectionString = configuration.GetConnectionString("analogagendastorage");
        if (!string.IsNullOrEmpty(aspireConnectionString))
        {
            return aspireConnectionString;
        }
        
        // Fallback: try to build from Storage config parts (already populated from appsettings)
        return BuildConnectionStringFromParts(storageCfg);
    }
    
    // Compute all values using static methods to avoid field initializer issues
    private static (string? connectionString, Uri accountUri, TokenCredential? credential) InitializeStorage(
        AzureAd azureAdCfg, Storage storageCfg, Configuration.Sections.System systemCfg, IConfiguration configuration, string serviceEndpoint)
    {
        var connectionString = GetConnectionString(storageCfg, configuration);
        var accountUri = ConstructAccountUri(storageCfg, systemCfg, serviceEndpoint);
        var credential = string.IsNullOrEmpty(connectionString)
            ? azureAdCfg.GetClientSecretCredential()
            : null;
        
        return (connectionString, accountUri, credential);
    }
    
    private readonly (string? connectionString, Uri accountUri, TokenCredential? credential) _storage = 
        InitializeStorage(azureAdCfg, storageCfg, systemCfg, configuration, serviceEndpoint);
    
    protected Uri AccountUri => _storage.accountUri;
    protected TokenCredential? Credential => _storage.credential;
    protected string? ConnectionString => _storage.connectionString;
    private readonly ConcurrentDictionary<string, TClient> _cache = new();

    /// <summary>
    /// Get or create a client for the specified resource name, with caching
    /// </summary>
    /// <param name="resourceName">The name of the resource (table name, container name, etc.)</param>
    /// <returns>The cached or newly created client</returns>
    protected TClient GetClient(string resourceName)
        => _cache.GetOrAdd(resourceName, CreateClient);

    /// <summary>
    /// Validate that the resource name is valid for this service type
    /// </summary>
    /// <param name="resourceName">The resource name to validate</param>
    /// <exception cref="ArgumentException">Thrown when the resource name is invalid</exception>
    protected abstract void ValidateResourceName(string resourceName);

    /// <summary>
    /// Create a new client instance for the specified resource name
    /// </summary>
    /// <param name="resourceName">The name of the resource</param>
    /// <returns>A new client instance</returns>
    protected abstract TClient CreateClient(string resourceName);

    /// <summary>
    /// Get a client with validation
    /// </summary>
    /// <param name="resourceName">The resource name</param>
    /// <returns>The client instance</returns>
    protected TClient GetValidatedClient(string resourceName)
    {
        ValidateResourceName(resourceName);
        return GetClient(resourceName);
    }
}
