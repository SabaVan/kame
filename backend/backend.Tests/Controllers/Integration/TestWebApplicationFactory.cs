using backend.Data;
using backend.Hubs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace backend.Tests.Controllers.Integration
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Add InMemory DbContext
                services.AddDbContext<AppDbContext>(options =>
          {
                  options.UseInMemoryDatabase("TestDb");
              });

                var mockHub = new Mock<IHubContext<BarHub>>();
                services.AddSingleton(mockHub.Object);
            });
        }
    }
}