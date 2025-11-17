using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Services;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services.Persistence;
using Microsoft.Maui.Storage;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Privatekonomi.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
        var storageSettings = builder.Configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();

        builder.Services.AddDbContext<PrivatekonomyContext>(options =>
        {
            var dbPath = string.IsNullOrWhiteSpace(storageSettings.ConnectionString)
                ? Path.Combine(FileSystem.AppDataDirectory, "privatekonomi_maui.db")
                : storageSettings.ConnectionString.Replace("Data Source=", string.Empty);
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Register core domain services
        builder.Services.AddScoped<ITransactionService, TransactionService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IBudgetService, BudgetService>();
        builder.Services.AddScoped<IGoalService, GoalService>();
        builder.Services.AddScoped<IPocketService, PocketService>();
        builder.Services.AddScoped<IInvestmentService, InvestmentService>();
        builder.Services.AddScoped<IAssetService, AssetService>();
        builder.Services.AddScoped<ILoanService, LoanService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<IDataPersistenceService, JsonFilePersistenceService>();

        builder.Logging.AddDebug();

        var app = builder.Build();
        App.Services = app.Services;

        using (var scope = app.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
            ctx.Database.EnsureCreated();
            if (storageSettings.SeedTestData)
            {
                TestDataSeeder.SeedTestDataOffline(ctx);
            }
        }

        return app;
    }
}
