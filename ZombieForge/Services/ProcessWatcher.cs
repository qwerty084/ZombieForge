using System;
using System.Management;

namespace ZombieForge.Services
{
    public class ProcessWatcher : IDisposable
    {
        private ManagementEventWatcher? _startWatcher;
        private ManagementEventWatcher? _stopWatcher;

        public event EventHandler? ProcessStarted;
        public event EventHandler? ProcessStopped;

        public void Watch(string processName)
        {
            _startWatcher = new ManagementEventWatcher(new WqlEventQuery(
                $"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{processName}'"));
            _startWatcher.EventArrived += (_, _) => ProcessStarted?.Invoke(this, EventArgs.Empty);
            _startWatcher.Start();

            _stopWatcher = new ManagementEventWatcher(new WqlEventQuery(
                $"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{processName}'"));
            _stopWatcher.EventArrived += (_, _) => ProcessStopped?.Invoke(this, EventArgs.Empty);
            _stopWatcher.Start();
        }

        public void Dispose()
        {
            _startWatcher?.Stop();
            _startWatcher?.Dispose();
            _stopWatcher?.Stop();
            _stopWatcher?.Dispose();
        }
    }
}
