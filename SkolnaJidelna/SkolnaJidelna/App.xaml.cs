using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkolniJidelna.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using SkolniJidelna.Data;
using Microsoft.EntityFrameworkCore;

namespace SkolniJidelna;
public partial class App : Application
{
    private readonly IHost _host;
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, conf) =>
            {
                conf.SetBasePath(AppContext.BaseDirectory);
                conf.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cfg = ctx.Configuration;

                // DbContext
                services.AddDbContext<AppDbContext>(options =>
                    options.UseOracle(cfg.GetConnectionString("OracleDb")));

                // ViewModels / Views
                services.AddTransient<StravnikListViewModel>();
                services.AddTransient<AdminViewModel>();
                services.AddTransient<AdminPanel>();

                // Login / Register viewmodels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<RegisterViewModel>();

                // Views (okna) přes DI
                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginWindow>(); // toto je registrační okno
                // services.AddTransient<RegisterWindow>(); // <- ODSTRANĚNO protože takové okno není
            })
            .Build();

        Services = _host.Services;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var main = Services.GetRequiredService<MainWindow>();

        // Nastavíme DataContext na LoginViewModel (MainWindow = přihlášení)
        main.DataContext = Services.GetRequiredService<LoginViewModel>();

        main.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}