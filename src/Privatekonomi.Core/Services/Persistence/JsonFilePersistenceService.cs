using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Privatekonomi.Core.Configuration;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;

namespace Privatekonomi.Core.Services.Persistence;

/// <summary>
/// JSON file-based persistence service for InMemory database
/// </summary>
public class JsonFilePersistenceService : IDataPersistenceService
{
    private readonly string _dataDirectory;
    private readonly ILogger<JsonFilePersistenceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFilePersistenceService(
        IOptions<StorageSettings> storageSettings,
        ILogger<JsonFilePersistenceService> logger)
    {
        _logger = logger;
        
        // Get data directory from ConnectionString or use default
        _dataDirectory = string.IsNullOrEmpty(storageSettings.Value.ConnectionString)
            ? Path.Combine(Directory.GetCurrentDirectory(), "data")
            : storageSettings.Value.ConnectionString;
        
        // Ensure directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        
        // Configure JSON serialization
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public bool Exists()
    {
        var files = new[]
        {
            "categories.json",
            "transactions.json",
            "budgets.json"
        };
        
        return files.Any(file => File.Exists(Path.Combine(_dataDirectory, file)));
    }

    public async Task SaveAsync(PrivatekonomyContext context)
    {
        try
        {
            _logger.LogInformation("Saving data to JSON files in {Directory}", _dataDirectory);

            // Save each entity type to a separate JSON file
            await SaveEntityAsync(context.Categories.ToList(), "categories.json");
            await SaveEntityAsync(context.Transactions.ToList(), "transactions.json");
            await SaveEntityAsync(context.TransactionCategories.ToList(), "transactioncategories.json");
            await SaveEntityAsync(context.CategoryRules.ToList(), "categoryrules.json");
            await SaveEntityAsync(context.Budgets.ToList(), "budgets.json");
            await SaveEntityAsync(context.BudgetCategories.ToList(), "budgetcategories.json");
            await SaveEntityAsync(context.Investments.ToList(), "investments.json");
            await SaveEntityAsync(context.Assets.ToList(), "assets.json");
            await SaveEntityAsync(context.Loans.ToList(), "loans.json");
            await SaveEntityAsync(context.Goals.ToList(), "goals.json");
            await SaveEntityAsync(context.BankSources.ToList(), "banksources.json");
            await SaveEntityAsync(context.BankConnections.ToList(), "bankconnections.json");
            await SaveEntityAsync(context.Households.ToList(), "households.json");
            await SaveEntityAsync(context.HouseholdMembers.ToList(), "householdmembers.json");
            await SaveEntityAsync(context.SharedExpenses.ToList(), "sharedexpenses.json");
            await SaveEntityAsync(context.ExpenseShares.ToList(), "expenseshares.json");
            await SaveEntityAsync(context.ChildAllowances.ToList(), "childallowances.json");
            await SaveEntityAsync(context.AllowanceTransactions.ToList(), "allowancetransactions.json");
            await SaveEntityAsync(context.AllowanceTasks.ToList(), "allowancetasks.json");
            await SaveEntityAsync(context.SalaryHistories.ToList(), "salaryhistories.json");
            await SaveEntityAsync(context.Pockets.ToList(), "pockets.json");
            await SaveEntityAsync(context.PocketTransactions.ToList(), "pockettransactions.json");
            await SaveEntityAsync(context.SharedGoals.ToList(), "sharedgoals.json");
            await SaveEntityAsync(context.SharedGoalParticipants.ToList(), "sharedgoalparticipants.json");
            await SaveEntityAsync(context.SharedGoalProposals.ToList(), "sharedgoalproposals.json");
            await SaveEntityAsync(context.SharedGoalProposalVotes.ToList(), "sharedgoalproposalvotes.json");
            await SaveEntityAsync(context.SharedGoalTransactions.ToList(), "sharedgoaltransactions.json");
            await SaveEntityAsync(context.SharedGoalNotifications.ToList(), "sharedgoalnotifications.json");
            await SaveEntityAsync(context.TaxDeductions.ToList(), "taxdeductions.json");
            await SaveEntityAsync(context.CapitalGains.ToList(), "capitalgains.json");
            await SaveEntityAsync(context.CommuteDeductions.ToList(), "commutedeductions.json");
            await SaveEntityAsync(context.CreditRatings.ToList(), "creditratings.json");
            await SaveEntityAsync(context.NetWorthSnapshots.ToList(), "networthsnapshots.json");
            await SaveEntityAsync(context.CurrencyAccounts.ToList(), "currencyaccounts.json");
            await SaveEntityAsync(context.AuditLogs.ToList(), "auditlogs.json");

            _logger.LogInformation("Data saved successfully to JSON files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to JSON files");
            throw;
        }
    }

    public async Task LoadAsync(PrivatekonomyContext context)
    {
        try
        {
            _logger.LogInformation("Loading data from JSON files in {Directory}", _dataDirectory);

            // Load in order to respect foreign key relationships
            await LoadEntityAsync<Category>(context, context.Categories, "categories.json");
            await LoadEntityAsync<BankSource>(context, context.BankSources, "banksources.json");
            await LoadEntityAsync<BankConnection>(context, context.BankConnections, "bankconnections.json");
            await LoadEntityAsync<Household>(context, context.Households, "households.json");
            await LoadEntityAsync<HouseholdMember>(context, context.HouseholdMembers, "householdmembers.json");
            await LoadEntityAsync<Transaction>(context, context.Transactions, "transactions.json");
            await LoadEntityAsync<TransactionCategory>(context, context.TransactionCategories, "transactioncategories.json");
            await LoadEntityAsync<CategoryRule>(context, context.CategoryRules, "categoryrules.json");
            await LoadEntityAsync<Budget>(context, context.Budgets, "budgets.json");
            await LoadEntityAsync<BudgetCategory>(context, context.BudgetCategories, "budgetcategories.json");
            await LoadEntityAsync<Investment>(context, context.Investments, "investments.json");
            await LoadEntityAsync<Asset>(context, context.Assets, "assets.json");
            await LoadEntityAsync<Loan>(context, context.Loans, "loans.json");
            await LoadEntityAsync<Goal>(context, context.Goals, "goals.json");
            await LoadEntityAsync<SharedExpense>(context, context.SharedExpenses, "sharedexpenses.json");
            await LoadEntityAsync<ExpenseShare>(context, context.ExpenseShares, "expenseshares.json");
            await LoadEntityAsync<ChildAllowance>(context, context.ChildAllowances, "childallowances.json");
            await LoadEntityAsync<AllowanceTransaction>(context, context.AllowanceTransactions, "allowancetransactions.json");
            await LoadEntityAsync<AllowanceTask>(context, context.AllowanceTasks, "allowancetasks.json");
            await LoadEntityAsync<SalaryHistory>(context, context.SalaryHistories, "salaryhistories.json");
            await LoadEntityAsync<Pocket>(context, context.Pockets, "pockets.json");
            await LoadEntityAsync<PocketTransaction>(context, context.PocketTransactions, "pockettransactions.json");
            await LoadEntityAsync<SharedGoal>(context, context.SharedGoals, "sharedgoals.json");
            await LoadEntityAsync<SharedGoalParticipant>(context, context.SharedGoalParticipants, "sharedgoalparticipants.json");
            await LoadEntityAsync<SharedGoalProposal>(context, context.SharedGoalProposals, "sharedgoalproposals.json");
            await LoadEntityAsync<SharedGoalProposalVote>(context, context.SharedGoalProposalVotes, "sharedgoalproposalvotes.json");
            await LoadEntityAsync<SharedGoalTransaction>(context, context.SharedGoalTransactions, "sharedgoaltransactions.json");
            await LoadEntityAsync<SharedGoalNotification>(context, context.SharedGoalNotifications, "sharedgoalnotifications.json");
            await LoadEntityAsync<TaxDeduction>(context, context.TaxDeductions, "taxdeductions.json");
            await LoadEntityAsync<CapitalGain>(context, context.CapitalGains, "capitalgains.json");
            await LoadEntityAsync<CommuteDeduction>(context, context.CommuteDeductions, "commutedeductions.json");
            await LoadEntityAsync<CreditRating>(context, context.CreditRatings, "creditratings.json");
            await LoadEntityAsync<NetWorthSnapshot>(context, context.NetWorthSnapshots, "networthsnapshots.json");
            await LoadEntityAsync<CurrencyAccount>(context, context.CurrencyAccounts, "currencyaccounts.json");
            await LoadEntityAsync<AuditLog>(context, context.AuditLogs, "auditlogs.json");

            _logger.LogInformation("Data loaded successfully from JSON files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from JSON files");
            throw;
        }
    }

    private async Task SaveEntityAsync<T>(List<T> entities, string fileName)
    {
        if (entities.Count == 0)
        {
            // Don't create empty files, but delete if exists
            var filePath = Path.Combine(_dataDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return;
        }

        var json = JsonSerializer.Serialize(entities, _jsonOptions);
        var path = Path.Combine(_dataDirectory, fileName);
        await File.WriteAllTextAsync(path, json);
    }

    private async Task LoadEntityAsync<T>(
        PrivatekonomyContext context,
        DbSet<T> dbSet,
        string fileName) where T : class
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(path);
        var entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
        
        if (entities != null && entities.Count > 0)
        {
            // Clear existing data to avoid conflicts
            var existingEntities = await dbSet.ToListAsync();
            if (existingEntities.Count > 0)
            {
                dbSet.RemoveRange(existingEntities);
                await context.SaveChangesAsync();
            }
            
            // Clear navigation properties to avoid duplicate tracking of related entities
            // Navigation properties may have been serialized, causing duplicate instances
            ClearNavigationProperties(entities);
            
            await dbSet.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Clears navigation properties from deserialized entities to avoid duplicate tracking.
    /// When entities are serialized with their navigation properties, deserializing creates
    /// duplicate instances that EF Core cannot track.
    /// </summary>
    private void ClearNavigationProperties<T>(List<T> entities) where T : class
    {
        if (entities == null || entities.Count == 0)
        {
            return;
        }

        var properties = typeof(T).GetProperties();
        foreach (var entity in entities)
        {
            foreach (var property in properties)
            {
                // Skip if not writable or if it's a primitive type
                if (!property.CanWrite || property.PropertyType.IsPrimitive || 
                    property.PropertyType == typeof(string) || 
                    property.PropertyType == typeof(decimal) ||
                    property.PropertyType == typeof(DateTime) ||
                    property.PropertyType == typeof(DateTime?))
                {
                    continue;
                }

                try
                {
                    var value = property.GetValue(entity);
                    if (value != null)
                    {
                        // Check if it's a collection property
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) &&
                            property.PropertyType != typeof(string))
                        {
                            // Create a new empty collection of the same type
                            var collectionType = property.PropertyType;
                            if (collectionType.IsInterface)
                            {
                                // If it's an interface like ICollection<T>, create a List<T>
                                var genericArgs = collectionType.GetGenericArguments();
                                if (genericArgs.Length > 0)
                                {
                                    var listType = typeof(List<>).MakeGenericType(genericArgs);
                                    var newCollection = Activator.CreateInstance(listType);
                                    property.SetValue(entity, newCollection);
                                }
                            }
                            else
                            {
                                // For concrete collection types, create a new instance
                                var newCollection = Activator.CreateInstance(collectionType);
                                property.SetValue(entity, newCollection);
                            }
                        }
                        else if (property.PropertyType.IsClass)
                        {
                            // Set reference properties to null
                            property.SetValue(entity, null);
                        }
                    }
                }
                catch
                {
                    // Skip any properties that fail to clear
                }
            }
        }
    }
}
