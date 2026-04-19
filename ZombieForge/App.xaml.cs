using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using ZombieForge.Services;

namespace ZombieForge
{
    /// <summary>
    /// Represents the WinUI application entry point and global application services.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the singleton main application window.
        /// </summary>
        public static MainWindow? MainWindow { get; private set; }

        /// <summary>
        /// Gets the shared logger factory used across the application.
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; } = CreateLoggerFactory();

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Starts the application and creates the main window.
        /// </summary>
        /// <param name="args">The launch activation arguments.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LocalizationService.Initialize();

            MainWindow = new MainWindow();
            MainWindow.Activate();
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
