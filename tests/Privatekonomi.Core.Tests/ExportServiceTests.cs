using Microsoft.EntityFrameworkCore;
using Moq;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using System.Text;
using Xunit;

namespace Privatekonomi.Core.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly ExportService _exportService;
    private readonly string _testUserId = "test-user-123";

    public ExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        _exportService = new ExportService(_context, _mockCurrentUserService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task SeedTestData()
    {
        // Add categories
        var category1 = new Category 
        { 
            CategoryId = 1, 
            Name = "Mat", 
            Color = "#FF0000"
        };
        var category2 = new Category 
        { 
            CategoryId = 2, 
            Name = "Lön", 
            Color = "#00FF00"
        };
        
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        // Add transactions for different years
        var transactions = new List<Transaction>
        {
            // Year 2023
            new Transaction
            {
                TransactionId = 1,
                Amount = 5000,
                Description = "Matinköp ICA",
                Date = new DateTime(2023, 1, 15),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 2,
                Amount = 30000,
                Description = "Lön januari",
                Date = new DateTime(2023, 1, 25),
                IsIncome = true,
                UserId = _testUserId,
                Currency = "SEK"
            },
            // Year 2024
            new Transaction
            {
                TransactionId = 3,
                Amount = 6000,
                Description = "Matinköp Willys",
                Date = new DateTime(2024, 2, 10),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 4,
                Amount = 32000,
                Description = "Lön februari",
                Date = new DateTime(2024, 2, 25),
                IsIncome = true,
                UserId = _testUserId,
                Currency = "SEK"
            },
            new Transaction
            {
                TransactionId = 5,
                Amount = 3500,
                Description = "Hyra",
                Date = new DateTime(2024, 3, 1),
                IsIncome = false,
                UserId = _testUserId,
                Currency = "SEK"
            }
        };

        _context.Transactions.AddRange(transactions);

        // Add budgets
        var budget2023 = new Budget
        {
            BudgetId = 1,
            Name = "Budget 2023",
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2023, 12, 31),
            Period = BudgetPeriod.Yearly,
            UserId = _testUserId
        };

        var budget2024 = new Budget
        {
            BudgetId = 2,
            Name = "Budget 2024",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = BudgetPeriod.Yearly,
            UserId = _testUserId
        };

        _context.Budgets.AddRange(budget2023, budget2024);

        // Add salary history
        var salaryHistory2023 = new SalaryHistory
        {
            SalaryHistoryId = 1,
            MonthlySalary = 30000,
            Period = new DateTime(2023, 1, 1),
            JobTitle = "Developer",
            UserId = _testUserId
        };

        var salaryHistory2024 = new SalaryHistory
        {
            SalaryHistoryId = 2,
            MonthlySalary = 32000,
            Period = new DateTime(2024, 1, 1),
            JobTitle = "Senior Developer",
            UserId = _testUserId
        };

        _context.SalaryHistories.AddRange(salaryHistory2023, salaryHistory2024);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvailableYearsAsync_ReturnsYearsWithTransactions()
    {
        // Arrange
        await SeedTestData();

        // Act
        var years = await _exportService.GetAvailableYearsAsync();

        // Assert
        Assert.NotNull(years);
        Assert.Equal(2, years.Count);
        Assert.Contains(2023, years);
        Assert.Contains(2024, years);
        Assert.Equal(2024, years[0]); // Should be sorted descending
        Assert.Equal(2023, years[1]);
    }

    [Fact]
    public async Task GetAvailableYearsAsync_NoTransactions_ReturnsEmptyList()
    {
        // Arrange - no data seeded

        // Act
        var years = await _exportService.GetAvailableYearsAsync();

        // Assert
        Assert.NotNull(years);
        Assert.Empty(years);
    }

    [Fact]
    public async Task ExportYearDataToJsonAsync_ValidYear_ExportsCorrectData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert
        Assert.NotNull(data);
        Assert.True(data.Length > 0);

        // Verify it's valid JSON and contains expected year and structure
        var json = Encoding.UTF8.GetString(data);
        Assert.Contains("\"year\": 2024", json);
        Assert.Contains("\"data\"", json.ToLower());
        Assert.Contains("\"transactions\"", json.ToLower());
        
        // Parse to verify it's valid JSON - skip BOM if present
        var jsonBytes = data;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            jsonBytes = data.Skip(3).ToArray();
        }
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonBytes);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(2024, root.GetProperty("year").GetInt32());
        var dataElement = root.GetProperty("data");
        var transactions = dataElement.GetProperty("transactions");
        
        // Should have 3 transactions for 2024
        Assert.Equal(3, transactions.GetArrayLength());
    }

    [Fact]
    public async Task ExportYearDataToCsvAsync_ValidYear_ExportsCorrectData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2024);

        // Assert
        Assert.NotNull(data);
        Assert.True(data.Length > 0);

        // Verify it's valid CSV with header
        var csv = Encoding.UTF8.GetString(data);
        Assert.Contains("# Privatekonomi Export - År 2024", csv);
        Assert.Contains("Datum,Beskrivning,Belopp,Typ", csv);
        Assert.Contains("Matinköp Willys", csv);
        Assert.Contains("Lön februari", csv);
        Assert.Contains("Hyra", csv);
        
        // Should not contain 2023 data
        Assert.DoesNotContain("Matinköp ICA", csv);
        Assert.DoesNotContain("Lön januari", csv);

        // Verify summary section
        Assert.Contains("# Summering 2024", csv);
        Assert.Contains("# Totala inkomster: 32000", csv);
        Assert.Contains("# Totala utgifter: 9500", csv); // 6000 + 3500
        Assert.Contains("# Nettoresultat: 22500", csv); // 32000 - 9500
    }

    [Fact]
    public async Task ExportYearDataToJsonAsync_IncludesAllRelevantData()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert
        var json = Encoding.UTF8.GetString(data);
        
        // Check for all data types (camelCase because of JsonNamingPolicy.CamelCase)
        Assert.Contains("transactions", json.ToLower());
        Assert.Contains("budgets", json.ToLower());
        Assert.Contains("goals", json.ToLower());
        Assert.Contains("investments", json.ToLower());
        Assert.Contains("loans", json.ToLower());
        // SalaryHistory is serialized as "salaryHistory" in camelCase
        Assert.Contains("\"salaryHistory\"", json);
        
        // Check for metadata (also in camelCase)
        Assert.Contains("\"year\": 2024", json);
        Assert.Contains("\"exportDate\"", json); // Property name in camelCase
        Assert.Contains("\"version\"", json);
    }

    [Fact]
    public async Task ExportYearDataToCsvAsync_CalculatesSummaryCorrectly()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2023);

        // Assert
        var csv = Encoding.UTF8.GetString(data).Replace(",",".");
        
        // Verify counts
        Assert.Contains("# Antal transaktioner: 2", csv);
        
        // Verify financial summary
        Assert.Contains("# Totala inkomster: 30000.00 SEK", csv);
        Assert.Contains("# Totala utgifter: 5000.00 SEK", csv);
        Assert.Contains("# Nettoresultat: 25000.00 SEK", csv);
    }

    [Fact]
    public async Task ExportYearDataToJsonAsync_FiltersByUserId()
    {
        // Arrange
        await SeedTestData();
        
        // Add transaction for different user
        var otherUserTransaction = new Transaction
        {
            TransactionId = 10,
            Amount = 99999,
            Description = "Other user transaction",
            Date = new DateTime(2024, 5, 1),
            IsIncome = true,
            UserId = "other-user-456",
            Currency = "SEK"
        };
        _context.Transactions.Add(otherUserTransaction);
        await _context.SaveChangesAsync();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2024);

        // Assert - skip BOM if present
        var jsonBytes = data;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            jsonBytes = data.Skip(3).ToArray();
        }
        
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonBytes);
        var root = jsonDoc.RootElement;
        var dataElement = root.GetProperty("data");
        var transactions = dataElement.GetProperty("transactions");
        
        // Should only have 3 transactions for current user (not the other user's transaction)
        Assert.Equal(3, transactions.GetArrayLength());
        
        // Verify none of the transactions have the other user's ID
        foreach (var transaction in transactions.EnumerateArray())
        {
            var userId = transaction.GetProperty("userId").GetString();
            Assert.Equal(_testUserId, userId);
        }
    }

    [Fact]
    public async Task ExportYearDataToCsvAsync_FiltersByUserId()
    {
        // Arrange
        await SeedTestData();
        
        // Add transaction for different user
        var otherUserTransaction = new Transaction
        {
            TransactionId = 10,
            Amount = 99999,
            Description = "Other user transaction",
            Date = new DateTime(2024, 5, 1),
            IsIncome = true,
            UserId = "other-user-456",
            Currency = "SEK"
        };
        _context.Transactions.Add(otherUserTransaction);
        await _context.SaveChangesAsync();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2024);

        // Assert
        var csv = Encoding.UTF8.GetString(data);
        
        // Should contain current user's data
        Assert.Contains("Matinköp Willys", csv);
        
        // Should NOT contain other user's data
        Assert.DoesNotContain("Other user transaction", csv);
    }

    [Fact]
    public async Task ExportYearDataToJsonAsync_YearWithNoData_ReturnsEmptyDataStructure()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToJsonAsync(2025); // No data for 2025

        // Assert
        Assert.NotNull(data);
        var json = Encoding.UTF8.GetString(data);
        
        // Should still have structure but empty arrays
        Assert.Contains("2025", json);
        Assert.Contains("transactions", json.ToLower());
    }

    [Fact]
    public async Task ExportYearDataToCsvAsync_YearWithNoData_ReturnsHeaderOnly()
    {
        // Arrange
        await SeedTestData();

        // Act
        var data = await _exportService.ExportYearDataToCsvAsync(2025); // No data for 2025

        // Assert
        Assert.NotNull(data);
        var csv = Encoding.UTF8.GetString(data).Replace(",",".");
        
        // Should have header with 0 transactions
        Assert.Contains("# Privatekonomi Export - År 2025", csv);
        Assert.Contains("# Antal transaktioner: 0", csv);
        Assert.Contains("# Totala inkomster: 0.00 SEK", csv);
        Assert.Contains("# Totala utgifter: 0.00 SEK", csv);
        Assert.Contains("# Nettoresultat: 0.00 SEK", csv);
    }
}
