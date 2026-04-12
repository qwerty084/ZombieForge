using System;
using System.Collections.Generic;
using System.Management;

namespace ZombieForge.Services
{
    public class ProcessWatcher : IDisposable
    {
        private readonly List<ManagementEventWatcher> _startWatchers = [];
        private readonly List<ManagementEventWatcher> _stopWatchers  = [];

        public event EventHandler? ProcessStarted;
        public event EventHandler? ProcessStopped;

        /// <summary>
        /// Starts watching all <paramref name="processNames"/> (without .exe).
        /// Any matching start/stop fires the respective event.
        /// </summary>
        public void Watch(IEnumerable<string> processNames)
        {
            foreach (var name in processNames)
            {
                string exe = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? name
                    : name + ".exe";

                var start = new ManagementEventWatcher(new WqlEventQuery(
                    $"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{exe}'"));
                start.EventArrived += (_, _) => ProcessStarted?.Invoke(this, EventArgs.Empty);
                start.Start();
                _startWatchers.Add(start);

                var stop = new ManagementEventWatcher(new WqlEventQuery(
                    $"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{exe}'"));
                stop.EventArrived += (_, _) => ProcessStopped?.Invoke(this, EventArgs.Empty);
                stop.Start();
                _stopWatchers.Add(stop);
            }
        }

        public void Dispose()
        {
            foreach (var w in _startWatchers) { w.Stop(); w.Dispose(); }
            foreach (var w in _stopWatchers)  { w.Stop(); w.Dispose(); }
        }
    }
}
