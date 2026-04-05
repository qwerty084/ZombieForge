using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace ZombieForge
{
    public partial class App : Application
    {
        private Window? _window;

        public static ILoggerFactory LoggerFactory { get; } = CreateLoggerFactory();

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZombieForge", "Logs", "log-.txt");

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(logPath,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);

            Log.Logger = loggerConfig.CreateLogger();

            return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddSerilog(Log.Logger));
        }
    }
}
