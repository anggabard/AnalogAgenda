using Database.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AnalogAgenda.Server.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<AnalogAgenda.Server.Controllers.AccountController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real services
            var tableServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITableService));
            if (tableServiceDescriptor != null)
            {
                services.Remove(tableServiceDescriptor);
            }

            var blobServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBlobService));
            if (blobServiceDescriptor != null)
            {
                services.Remove(blobServiceDescriptor);
            }

            // Add mock services
            var mockTableService = new Mock<ITableService>();
            var mockBlobService = new Mock<IBlobService>();

            // Configure mock behaviors here as needed
            mockTableService.Setup(x => x.GetTableEntriesAsync<Database.Entities.UserEntity>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Database.Entities.UserEntity, bool>>>()))
                .ReturnsAsync(new List<Database.Entities.UserEntity>());

            services.AddSingleton(mockTableService.Object);
            services.AddSingleton(mockBlobService.Object);
        });

        // Use testing environment to disable rate limiting
        builder.UseEnvironment("Testing");
    }
}
