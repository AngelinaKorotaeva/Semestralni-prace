using System.Windows;
using Microsoft.Extensions.Hosting;
using SkolniJidelna.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using SkolniJidelna.Data;
using SkolniJidelna.Services;
using Microsoft.EntityFrameworkCore;

namespace SkolniJidelna;
public partial class App : Application
{
    private readonly IHost _host;
    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, conf) =>
            {
                conf.SetBasePath(AppContext.BaseDirectory);
                conf.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var cfg = ctx.Configuration;
                services.AddDbContext<AppDbContext>(options =>
                    options.UseOracle(cfg.GetConnectionString("OracleDb")));
                services.AddScoped<IStravnikRepository, StravnikRepository>();

                // ViewModels / Views
                services.AddTransient<StravnikListViewModel>();
                services.AddTransient<AdminViewModel>();
                services.AddTransient<AdminPanel>();

                // hlavní okno
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        var main = _host.Services.GetRequiredService<MainWindow>();
        main.DataContext = _host.Services.GetRequiredService<StravnikListViewModel>();
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