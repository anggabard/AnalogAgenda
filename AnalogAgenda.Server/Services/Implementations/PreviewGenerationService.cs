namespace AnalogAgenda.Server.Services.Implementations;

/// <summary>
/// Service to limit concurrent preview generation and prevent memory exhaustion
/// </summary>
public class PreviewGenerationService
{
    // Limit to 2 concurrent preview generations to prevent memory issues
    private readonly SemaphoreSlim _semaphore = new(2, 2);
    
    /// <summary>
    /// Execute preview generation with concurrency limit
    /// </summary>
    public async Task<T> ExecuteWithLimitAsync<T>(Func<Task<T>> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

