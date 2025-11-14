using Microsoft.EntityFrameworkCore;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Models;
using Privatekonomi.Core.Services;
using Xunit;

namespace Privatekonomi.Core.Tests;

public class HouseholdServiceTests : IDisposable
{
    private readonly PrivatekonomyContext _context;
    private readonly HouseholdService _householdService;

    public HouseholdServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrivatekonomyContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PrivatekonomyContext(options);
        _householdService = new HouseholdService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Household CRUD Tests

    [Fact]
    public async Task UpdateHouseholdAsync_UpdatesHouseholdSuccessfully()
    {
        // Arrange
        var household = new Household 
        { 
            Name = "Original Name",
            Description = "Original Description"
        };
        var created = await _householdService.CreateHouseholdAsync(household);

        // Act
        created.Name = "Updated Name";
        created.Description = "Updated Description";
        var updated = await _householdService.UpdateHouseholdAsync(created);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
        Assert.Equal(created.HouseholdId, updated.HouseholdId);

        // Verify the update persisted
        var retrieved = await _householdService.GetHouseholdByIdAsync(created.HouseholdId);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal("Updated Description", retrieved.Description);
    }

    #endregion

    #region Activity Tests

    [Fact]
    public async Task CreateActivityAsync_CreatesActivitySuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Cleaned the kitchen",
            Description = "Deep cleaning",
            Type = HouseholdActivityType.Cleaning
        };

        // Act
        var result = await _householdService.CreateActivityAsync(activity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cleaned the kitchen", result.Title);
        Assert.Equal(HouseholdActivityType.Cleaning, result.Type);
        Assert.NotEqual(default, result.CreatedDate);
        Assert.NotEqual(default, result.CompletedDate);
    }

    [Fact]
    public async Task GetActivitiesAsync_ReturnsActivitiesOrderedByDate()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity1 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Activity 1",
            CompletedDate = DateTime.Now.AddDays(-2)
        };
        var activity2 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Activity 2",
            CompletedDate = DateTime.Now.AddDays(-1)
        };

        await _householdService.CreateActivityAsync(activity1);
        await _householdService.CreateActivityAsync(activity2);

        // Act
        var result = await _householdService.GetActivitiesAsync(household.HouseholdId);

        // Assert
        var activities = result.ToList();
        Assert.Equal(2, activities.Count);
        Assert.Equal("Activity 2", activities[0].Title); // Most recent first
        Assert.Equal("Activity 1", activities[1].Title);
    }

    [Fact]
    public async Task GetActivitiesAsync_FiltersWithDateRange()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity1 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Old Activity",
            CompletedDate = DateTime.Now.AddMonths(-2)
        };
        var activity2 = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Recent Activity",
            CompletedDate = DateTime.Now
        };

        await _householdService.CreateActivityAsync(activity1);
        await _householdService.CreateActivityAsync(activity2);

        // Act
        var result = await _householdService.GetActivitiesAsync(
            household.HouseholdId,
            startDate: DateTime.Now.AddDays(-1)
        );

        // Assert
        var activities = result.ToList();
        Assert.Single(activities);
        Assert.Equal("Recent Activity", activities[0].Title);
    }

    [Fact]
    public async Task DeleteActivityAsync_RemovesActivity()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var activity = new HouseholdActivity
        {
            HouseholdId = household.HouseholdId,
            Title = "Test Activity"
        };
        var created = await _householdService.CreateActivityAsync(activity);

        // Act
        var result = await _householdService.DeleteActivityAsync(created.HouseholdActivityId);

        // Assert
        Assert.True(result);
        var activities = await _householdService.GetActivitiesAsync(household.HouseholdId);
        Assert.Empty(activities);
    }

    #endregion

    #region Task Tests

    [Fact]
    public async Task CreateTaskAsync_CreatesTaskSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Buy groceries",
            Description = "Milk, eggs, bread",
            Priority = HouseholdTaskPriority.High,
            DueDate = DateTime.Now.AddDays(1)
        };

        // Act
        var result = await _householdService.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Buy groceries", result.Title);
        Assert.Equal(HouseholdTaskPriority.High, result.Priority);
        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedDate);
        Assert.NotEqual(default, result.CreatedDate);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsTasksOrderedByPriority()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Low priority task",
            Priority = HouseholdTaskPriority.Low
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "High priority task",
            Priority = HouseholdTaskPriority.High
        };

        await _householdService.CreateTaskAsync(task1);
        await _householdService.CreateTaskAsync(task2);

        // Act
        var result = await _householdService.GetTasksAsync(household.HouseholdId);

        // Assert
        var tasks = result.ToList();
        Assert.Equal(2, tasks.Count);
        Assert.Equal("High priority task", tasks[0].Title); // Higher priority first
    }

    [Fact]
    public async Task GetTasksAsync_ExcludesCompletedTasksWhenRequested()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Incomplete task"
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Completed task"
        };

        await _householdService.CreateTaskAsync(task1);
        var created = await _householdService.CreateTaskAsync(task2);
        await _householdService.MarkTaskCompleteAsync(created.HouseholdTaskId);

        // Act
        var result = await _householdService.GetTasksAsync(household.HouseholdId, includeCompleted: false);

        // Assert
        var tasks = result.ToList();
        Assert.Single(tasks);
        Assert.Equal("Incomplete task", tasks[0].Title);
    }

    [Fact]
    public async Task MarkTaskCompleteAsync_MarksTaskAsCompleted()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        var member = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "John Doe"
        };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.MarkTaskCompleteAsync(
            created.HouseholdTaskId,
            member.HouseholdMemberId
        );

        // Assert
        Assert.True(result);
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.NotNull(updated);
        Assert.True(updated.IsCompleted);
        Assert.NotNull(updated.CompletedDate);
        Assert.Equal(member.HouseholdMemberId, updated.CompletedByMemberId);
    }

    [Fact]
    public async Task MarkTaskIncompleteAsync_MarksTaskAsIncomplete()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);
        await _householdService.MarkTaskCompleteAsync(created.HouseholdTaskId);

        // Act
        var result = await _householdService.MarkTaskIncompleteAsync(created.HouseholdTaskId);

        // Assert
        Assert.True(result);
        var updated = await _context.HouseholdTasks.FindAsync(created.HouseholdTaskId);
        Assert.NotNull(updated);
        Assert.False(updated.IsCompleted);
        Assert.Null(updated.CompletedDate);
        Assert.Null(updated.CompletedByMemberId);
    }

    [Fact]
    public async Task SearchTasksAsync_FindsTasksByTitle()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task1 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Buy groceries"
        };
        var task2 = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Clean house"
        };

        await _householdService.CreateTaskAsync(task1);
        await _householdService.CreateTaskAsync(task2);

        // Act
        var result = await _householdService.SearchTasksAsync(household.HouseholdId, "groceries");

        // Assert
        var tasks = result.ToList();
        Assert.Single(tasks);
        Assert.Equal("Buy groceries", tasks[0].Title);
    }

    [Fact]
    public async Task SearchTasksAsync_FindsTasksByDescription()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Shopping",
            Description = "Buy milk and eggs"
        };

        await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.SearchTasksAsync(household.HouseholdId, "milk");

        // Assert
        var tasks = result.ToList();
        Assert.Single(tasks);
        Assert.Equal("Shopping", tasks[0].Title);
    }

    [Fact]
    public async Task DeleteTaskAsync_RemovesTask()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        var task = new HouseholdTask
        {
            HouseholdId = household.HouseholdId,
            Title = "Test task"
        };
        var created = await _householdService.CreateTaskAsync(task);

        // Act
        var result = await _householdService.DeleteTaskAsync(created.HouseholdTaskId);

        // Assert
        Assert.True(result);
        var tasks = await _householdService.GetTasksAsync(household.HouseholdId);
        Assert.Empty(tasks);
    }

    #endregion

    #region Shared Budget Tests

    [Fact]
    public async Task CreateSharedBudgetAsync_CreatesSharedBudgetSuccessfully()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member1 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        var member2 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 2",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member1);
        _context.HouseholdMembers.Add(member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            Description = "Test shared budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 60m },
            { member2.HouseholdMemberId, 40m }
        };

        // Act
        var result = await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Budget", result.Name);
        Assert.Equal(household.HouseholdId, result.HouseholdId);
        Assert.Equal("test-user-id", result.UserId);
        
        // Verify budget shares were created
        var shares = await _context.HouseholdBudgetShares
            .Where(s => s.BudgetId == result.BudgetId)
            .ToListAsync();
        Assert.Equal(2, shares.Count);
        Assert.Contains(shares, s => s.HouseholdMemberId == member1.HouseholdMemberId && s.SharePercentage == 60m);
        Assert.Contains(shares, s => s.HouseholdMemberId == member2.HouseholdMemberId && s.SharePercentage == 40m);
    }

    [Fact]
    public async Task CreateSharedBudgetAsync_ThrowsWhenContributionsDontSumTo100()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member1 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        var member2 = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 2",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member1);
        _context.HouseholdMembers.Add(member2);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member1.HouseholdMemberId, 60m },
            { member2.HouseholdMemberId, 30m } // Total is 90%, not 100%
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _householdService.CreateSharedBudgetAsync(budget, contributions)
        );
        Assert.Contains("must sum to 100%", exception.Message);
    }

    [Fact]
    public async Task CreateSharedBudgetAsync_ThrowsWhenHouseholdIdIsNull()
    {
        // Arrange
        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = null, // No household ID
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _householdService.CreateSharedBudgetAsync(budget, contributions)
        );
        Assert.Contains("must be associated with a household", exception.Message);
    }

    [Fact]
    public async Task GetHouseholdBudgetsAsync_ReturnsHouseholdBudgets()
    {
        // Arrange
        var household = new Household { Name = "Test Household" };
        _context.Households.Add(household);
        
        var member = new HouseholdMember
        {
            HouseholdId = household.HouseholdId,
            Name = "Member 1",
            IsActive = true
        };
        _context.HouseholdMembers.Add(member);
        await _context.SaveChangesAsync();

        var budget = new Budget
        {
            Name = "Test Budget",
            HouseholdId = household.HouseholdId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Period = BudgetPeriod.Monthly,
            UserId = "test-user-id"
        };

        var contributions = new Dictionary<int, decimal>
        {
            { member.HouseholdMemberId, 100m }
        };

        await _householdService.CreateSharedBudgetAsync(budget, contributions);

        // Act
        var result = await _householdService.GetHouseholdBudgetsAsync(household.HouseholdId);

        // Assert
        var budgets = result.ToList();
        Assert.Single(budgets);
        Assert.Equal("Test Budget", budgets[0].Name);
        Assert.NotEmpty(budgets[0].HouseholdBudgetShares);
    }

    #endregion
}
