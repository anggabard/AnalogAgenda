using AnalogAgenda.Server.Services.Implementations;
using Xunit;

namespace AnalogAgenda.Server.Tests.Services;

public class PreviewGenerationServiceTests
{
    [Fact]
    public async Task ExecuteWithLimitAsync_SingleOperation_ExecutesSuccessfully()
    {
        // Arrange
        var service = new PreviewGenerationService();
        var executed = false;

        // Act
        var result = await service.ExecuteWithLimitAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
            return "success";
        });

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task ExecuteWithLimitAsync_ConcurrentOperations_LimitsTo2Concurrent()
    {
        // Arrange
        var service = new PreviewGenerationService();
        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockObj = new object();

        // Act - Start 5 operations simultaneously
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            return await service.ExecuteWithLimitAsync(async () =>
            {
                lock (lockObj)
                {
                    currentConcurrent++;
                    if (currentConcurrent > maxConcurrent)
                    {
                        maxConcurrent = currentConcurrent;
                    }
                }

                // Simulate work
                await Task.Delay(50);

                lock (lockObj)
                {
                    currentConcurrent--;
                }

                return i;
            });
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Maximum concurrent should never exceed 2 (semaphore limit)
        Assert.True(maxConcurrent <= 2, $"Expected max 2 concurrent, but got {maxConcurrent}");
    }

    [Fact]
    public async Task ExecuteWithLimitAsync_OperationThrowsException_ReleasesLock()
    {
        // Arrange
        var service = new PreviewGenerationService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.ExecuteWithLimitAsync<int>(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test exception");
            });
        });

        // Verify semaphore was released by successfully running another operation
        var executed = false;
        await service.ExecuteWithLimitAsync(async () =>
        {
            executed = true;
            return true;
        });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteWithLimitAsync_MultipleOperationsInSequence_AllComplete()
    {
        // Arrange
        var service = new PreviewGenerationService();
        var results = new List<int>();

        // Act - Execute 10 operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            var result = await service.ExecuteWithLimitAsync(async () =>
            {
                await Task.Delay(5);
                return index;
            });
            results.Add(result);
        }

        // Assert - All operations completed in order
        Assert.Equal(10, results.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(i, results[i]);
        }
    }
}

