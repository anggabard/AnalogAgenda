using Azure.Core;
using Configuration.Sections;
using Database.Helpers;
using System.Collections.Concurrent;

namespace Database.Services;

/// <summary>
/// Base class for Azure services that provides common caching and client creation patterns
/// </summary>
/// <typeparam name="TClient">The type of Azure client to create and cache</typeparam>
public abstract class BaseAzureService<TClient>(AzureAd azureAdCfg, Storage storageCfg, string serviceEndpoint) where TClient : class
{
    protected readonly Uri AccountUri = new($"https://{storageCfg.AccountName}.{serviceEndpoint}.core.windows.net");
    protected readonly TokenCredential Credential = azureAdCfg.GetClientSecretCredential();
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
