using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class HomeViewModel
    {
        private readonly BlackOps1Handler _handler = new();
        private readonly DispatcherTimer _timer;
        private readonly ILogger<HomeViewModel> _logger;

        public HomeViewModel()
        {
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, object e)
        {
            var processes = Process.GetProcessesByName("BlackOps");
            if (processes.Length == 0) return;

            var proc = processes[0];

            long moduleBase;
            try { moduleBase = (long)proc.MainModule!.BaseAddress; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read module base for PID={Pid}", proc.Id);
                return;
            }

            var handle = MemoryService.OpenGameProcess(proc.Id);
            if (handle == IntPtr.Zero)
            {
                _logger.LogWarning("OpenProcess failed for PID={Pid}, Win32Error={Error}", proc.Id, Marshal.GetLastWin32Error());
                return;
            }

            try
            {
                var stats = _handler.ReadPlayerStats(handle, moduleBase, 0);
                _logger.LogDebug("Stats: Points={Points} Kills={Kills} Downs={Downs} Headshots={Headshots}",
                    stats.Points, stats.Kills, stats.Downs, stats.Headshots);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read player stats");
            }
            finally
            {
                MemoryService.CloseGameProcess(handle);
            }
        }
    }
}
