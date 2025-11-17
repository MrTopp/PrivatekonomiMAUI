using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Privatekonomi.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
	public static IServiceProvider Services { get; internal set; } = default!;

	public App()
	{
		InitializeComponent();
		this.MainPage = new AppShell();
	}
}