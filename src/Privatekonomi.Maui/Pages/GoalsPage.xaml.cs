using Microsoft.Maui.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Core.Data;

namespace Privatekonomi.Maui.Pages;

public partial class GoalsPage : ContentPage
{
    private readonly PrivatekonomyContext _ctx;
    public GoalsPage()
    {
        InitializeComponent();
        _ctx = App.Services.GetRequiredService<PrivatekonomyContext>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var data = await _ctx.Goals.OrderBy(g => g.Priority).ToListAsync();
        Goals.ItemsSource = data.Select(g => new
        {
            g.Name,
            Progress = g.TargetAmount == 0 ? 0 : (double)(g.CurrentAmount / g.TargetAmount),
            CurrentAmountFormatted = $"{g.CurrentAmount:C0} av {g.TargetAmount:C0}"
        }).ToList();
    }
}