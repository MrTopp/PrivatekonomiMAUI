using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Xunit;

namespace Privatekonomi.Core.Tests;

public class StorageConfigurationTests
{
    private static IServiceProvider CreateServiceProvider(Dictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(); // Add logging for persistence services
        services.AddPrivatekonomyStorage(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void InMemoryStorage_ShouldBeConfigurable()
    {
        // Arrange & Act
        var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "InMemory",
            ["Storage:ConnectionString"] = "",
            ["Storage:SeedTestData"] = "false"
        });
        var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.NotNull(context);
        Assert.True(context.Database.IsInMemory());
    }

    [Fact]
    public void SqliteStorage_ShouldBeConfigurable()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        
        // Act
        var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Sqlite",
            ["Storage:ConnectionString"] = $"Data Source={dbPath}",
            ["Storage:SeedTestData"] = "false"
        });
        var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.NotNull(context);
        Assert.False(context.Database.IsInMemory());
        Assert.True(context.Database.IsSqlite());

        // Cleanup
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [Fact]
    public void StorageSettings_ShouldBindFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = "Data Source=test.db",
                ["Storage:SeedTestData"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageSettings>>().Value;

        // Assert
        Assert.Equal("Sqlite", settings.Provider);
        Assert.Equal("Data Source=test.db", settings.ConnectionString);
        Assert.True(settings.SeedTestData);
    }

    [Fact]
    public async Task SqliteStorage_ShouldPersistData()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

        // Act - Create database and add data
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = $"Data Source={dbPath}",
                ["Storage:SeedTestData"] = "false"
            });
            try
            {
                var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
                await using (context)
                {
                    await context.Database.EnsureCreatedAsync();

                    context.Categories.Add(new Core.Models.Category
                    {
                        Name = "Test Category",
                        Color = "#FF0000",
                        TaxRelated = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
            finally
            {
                if (serviceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        // Act - Retrieve data with new context
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Sqlite",
                ["Storage:ConnectionString"] = $"Data Source={dbPath}",
                ["Storage:SeedTestData"] = "false"
            });
            try
            {
                var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
                await using (context)
                {
                    var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Test Category");

                    // Assert
                    Assert.NotNull(category);
                    Assert.Equal("Test Category", category.Name);
                    Assert.Equal("#FF0000", category.Color);
                }
            }
            finally
            {
                if (serviceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        // Cleanup
        if (File.Exists(dbPath))
        {
            SqliteConnection.ClearAllPools();
            
            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    File.Delete(dbPath);
                    break;
                }
                catch (IOException) when (retries > 1)
                {
                    System.Threading.Thread.Sleep(100);
                    retries--;
                }
            }
        }
    }

    [Fact]
    public void SqlServerStorage_ShouldBeConfigurable()
    {
        // Arrange
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=PrivatekonomyTest;Trusted_Connection=True;MultipleActiveResultSets=true";
        
        // Act
        var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "SqlServer",
            ["Storage:ConnectionString"] = connectionString,
            ["Storage:SeedTestData"] = "false"
        });
        var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.NotNull(context);
        Assert.False(context.Database.IsInMemory());
        Assert.True(context.Database.IsSqlServer());
    }

    [Fact]
    public void SqlServerStorage_WithoutConnectionString_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "SqlServer",
                ["Storage:ConnectionString"] = "",
                ["Storage:SeedTestData"] = "false"
            });
            // Exception is thrown during AddDbContext configuration
        });

        Assert.Contains("ConnectionString is required for SqlServer provider", exception.Message);
    }

    [Fact]
    public void JsonFileStorage_ShouldBeConfigurable()
    {
        // Arrange & Act
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");
        var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "JsonFile",
            ["Storage:ConnectionString"] = dataPath,
            ["Storage:SeedTestData"] = "false"
        });
        var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();

        // Assert
        Assert.NotNull(context);
        Assert.True(context.Database.IsInMemory());
        
        // Verify persistence service is registered
        var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();
        Assert.NotNull(persistenceService);

        // Cleanup
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }
    }

    [Fact]
    public async Task JsonFileStorage_ShouldPersistAndLoadData()
    {
        // Arrange
        var dataPath = Path.Combine(Path.GetTempPath(), $"jsontest_{Guid.NewGuid()}");

        // Act - Create data and save
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "JsonFile",
                ["Storage:ConnectionString"] = dataPath,
                ["Storage:SeedTestData"] = "false"
            });
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();

            Assert.NotNull(persistenceService); // Verify service is registered

            await context.Database.EnsureCreatedAsync();

            context.Categories.Add(new Core.Models.Category
            {
                Name = "Test Category",
                Color = "#FF0000",
                TaxRelated = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Save to JSON files
            await persistenceService.SaveAsync(context);
        }

        // Act - Load data in new context
        {
            var serviceProvider = CreateServiceProvider(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "JsonFile",
                ["Storage:ConnectionString"] = dataPath,
                ["Storage:SeedTestData"] = "false"
            });
            var context = serviceProvider.GetRequiredService<PrivatekonomyContext>();
            var persistenceService = serviceProvider.GetService<Core.Services.Persistence.IDataPersistenceService>();

            Assert.NotNull(persistenceService); // Verify service is registered

            await context.Database.EnsureCreatedAsync();
            await persistenceService.LoadAsync(context);

            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Test Category");

            // Assert
            Assert.NotNull(category);
            Assert.Equal("Test Category", category.Name);
            Assert.Equal("#FF0000", category.Color);
        }

        // Cleanup
        if (Directory.Exists(dataPath))
        {
            Directory.Delete(dataPath, true);
        }
    }
}
