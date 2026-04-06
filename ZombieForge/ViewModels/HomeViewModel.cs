using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class HomeViewModel : IDisposable
    {
        private readonly BlackOps1Handler _handler = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger<HomeViewModel> _logger;

        public HomeViewModel()
        {
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _ = PollAsync(_cts.Token);
        }

        private async Task PollAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                    Poll();
            }
            catch (OperationCanceledException) { }
        }

        private void Poll()
        {
            var processes = Process.GetProcessesByName(_handler.ProcessName);
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

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
