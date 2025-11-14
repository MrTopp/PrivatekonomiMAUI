using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Privatekonomi.Web.Components;
using Privatekonomi.Web.Components.Account;
using Privatekonomi.Web.Services;
using System.Globalization;
using Privatekonomi.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() || string.Equals(builder.Environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

// Raspberry Pi configuration - ensure we listen on all network interfaces
var isRaspberryPi = Environment.GetEnvironmentVariable("PRIVATEKONOMI_RASPBERRY_PI") == "true";
if (isRaspberryPi)
{
    // When running under Aspire, it manages the ports via WithHttpEndpoint
    // But Aspire binds to localhost by default, so we need to override this
    // We configure Kestrel AFTER AddServiceDefaults to ensure our binding takes precedence
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        // Listen on all network interfaces (0.0.0.0) for Raspberry Pi network access
        // Get port from Aspire's PORT env var, or fall back to configuration, or default to 5274
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5274;
        serverOptions.ListenAnyIP(port);
    });
}

// Configure Swedish culture as default
var swedishCulture = new CultureInfo("sv-SE");
CultureInfo.DefaultThreadCurrentCulture = swedishCulture;
CultureInfo.DefaultThreadCurrentUICulture = swedishCulture;

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add SignalR for real-time budget alerts
builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Blazor Server options for better error handling
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DetailedErrors = true;
    }
});

// Add MudBlazor services with configuration for better accessibility
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Configure storage based on appsettings
builder.Services.AddPrivatekonomyStorage(builder.Configuration);

// Add Identity services
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorizationBuilder();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<PrivatekonomyContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Add HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Conditionally register CurrentUserService based on DevDisableAuth feature flag
// This allows temporarily bypassing authentication in development for faster testing
if (builder.Environment.IsDevelopment() && 
    builder.Configuration.GetValue<bool>("FeatureFlags:DevDisableAuth"))
{
    builder.Services.AddScoped<ICurrentUserService, DevCurrentUserService>();
}
else
{
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
}

// Register services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRuleService, CategoryRuleService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDebtStrategyService, DebtStrategyService>();
builder.Services.AddScoped<IMortgageAnalysisService, MortgageAnalysisService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IBankSourceService, BankSourceService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IChildAllowanceService, ChildAllowanceService>();
builder.Services.AddScoped<IGoalMilestoneService, GoalMilestoneService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IRoundUpService, RoundUpService>();
builder.Services.AddScoped<IPocketService, PocketService>();
builder.Services.AddScoped<ISharedGoalService, SharedGoalService>();
builder.Services.AddScoped<ISocialFeatureService, SocialFeatureService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IHeatmapAnalysisService, HeatmapAnalysisService>();
builder.Services.AddScoped<ISalaryHistoryService, SalaryHistoryService>();
builder.Services.AddScoped<ICurrencyAccountService, CurrencyAccountService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPensionService, PensionService>();
builder.Services.AddScoped<IDividendService, DividendService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IBudgetAlertService, BudgetAlertService>();
builder.Services.AddScoped<ILifeTimelinePlannerService, LifeTimelinePlannerService>();
builder.Services.AddScoped<ISavingsChallengeService, SavingsChallengeService>();
builder.Services.AddScoped<IKonsumentverketComparisonService, KonsumentverketComparisonService>();
builder.Services.AddScoped<IKalpService, KalpService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<ICashFlowScenarioService, CashFlowScenarioService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DashboardPreferencesService>();
builder.Services.AddScoped<IDashboardLayoutService, DashboardLayoutService>();
builder.Services.AddScoped<ViewDensityService>();

// Register background services
builder.Services.AddHostedService<Privatekonomi.Web.Services.BudgetAlertBackgroundService>();
builder.Services.AddHostedService<Privatekonomi.Web.Services.WeeklyBudgetDigestService>();

// Swedish-specific services
builder.Services.AddScoped<ISieExporter, SieExporter>();
builder.Services.AddScoped<IK4Generator, K4Generator>();
builder.Services.AddScoped<ITaxDeductionService, TaxDeductionService>();
builder.Services.AddScoped<IISKTaxCalculator, ISKTaxCalculator>();

// Register bank API services and dependencies
builder.Services.AddBankApiServices(builder.Configuration);

// Add HttpClient for API calls
builder.Services.AddHttpClient("api", client =>
{
    // Configure API base address based on environment
    // Aspire will override this through service discovery when running with AppHost
    // For Raspberry Pi and standalone deployments, use configuration
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] 
        ?? builder.Configuration["services:api:http:0"] 
        ?? "http://localhost:5277";
    
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Configure default HttpClient to use the API client configuration
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    // Use the configured "api" client as the default
    return httpClientFactory.CreateClient("api");
});

// Register stock price service with HttpClient
builder.Services.AddHttpClient<IStockPriceService, YahooFinanceStockPriceService>();

var app = builder.Build();

// Configure request localization for Swedish
var supportedCultures = new[] { new CultureInfo("sv-SE") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sv-SE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Seed the database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var storageSettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageSettings>>().Value;
        
        var context = scope.ServiceProvider.GetRequiredService<PrivatekonomyContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // For SQLite and SqlServer, ensure database is created
        if (storageSettings.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) ||
            storageSettings.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Using {Provider} storage, ensuring database is created...", storageSettings.Provider);
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.EnsureCreated();
        }
        
        // For JsonFile, load existing data
        if (storageSettings.Provider.Equals("JsonFile", StringComparison.OrdinalIgnoreCase))
        {
            var persistenceService = scope.ServiceProvider.GetService<Privatekonomi.Core.Services.Persistence.IDataPersistenceService>();
            if (persistenceService != null && persistenceService.Exists())
            {
                logger.LogInformation("Loading data from JSON files...");
                await persistenceService.LoadAsync(context);
                logger.LogInformation("Data loaded successfully from JSON files");
            }
        }
        
        // Seed test data only if configured
        if (storageSettings.SeedTestData)
        {
            logger.LogInformation("Seeding test data...");
            await TestDataSeeder.SeedTestDataAsync(context, userManager);
            logger.LogInformation("Database seeding completed successfully");
        }
        else
        {
            logger.LogInformation("Test data seeding is disabled in configuration");
        }
    }
}
catch (Exception ex)
{
    // Log the error but don't crash the application
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database. Application will continue without test data.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add Identity endpoints
app.MapGroup("/Account").MapIdentityApi<ApplicationUser>();

// Map SignalR hubs
app.MapHub<Privatekonomi.Web.Hubs.BudgetAlertHub>("/hubs/budgetalert");

app.Run();
