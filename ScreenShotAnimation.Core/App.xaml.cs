using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ScreenShotAnimation.Models;
using ScreenShotAnimation.ViewModels;
using ScreenShotAnimation.Views;
using System.Windows;

namespace ScreenShotAnimation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
                    services.AddTransient<MiniWindow>();
                    services.AddTransient<MiniViewModel>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<MainViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            var appSettings = AppHost.Services.GetRequiredService<IOptions<AppSettings>>().Value;
            Window startup = appSettings.FormStyle == 0 ? AppHost.Services.GetRequiredService<MainWindow>() : AppHost.Services.GetRequiredService<MiniWindow>();
            startup.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (AppHost)
            {
                await AppHost.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
