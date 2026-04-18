using System;
using System.Collections.Generic;
using System.IO;
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
            ArgumentNullException.ThrowIfNull(processNames);

            foreach (var name in processNames)
            {
                string exe = NormalizeProcessName(name);
                string safeExe = EscapeWqlStringLiteral(exe);

                var start = new ManagementEventWatcher(new WqlEventQuery(
                    $"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{safeExe}'"));
                start.EventArrived += (_, _) => ProcessStarted?.Invoke(this, EventArgs.Empty);
                start.Start();
                _startWatchers.Add(start);

                var stop = new ManagementEventWatcher(new WqlEventQuery(
                    $"SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{safeExe}'"));
                stop.EventArrived += (_, _) => ProcessStopped?.Invoke(this, EventArgs.Empty);
                stop.Start();
                _stopWatchers.Add(stop);
            }
        }

        internal static string NormalizeProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                throw new ArgumentException("Process name cannot be null, empty, or whitespace.", nameof(processName));
            }

            string trimmed = processName.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException("Process name contains invalid file name characters.", nameof(processName));
            }

            string extension = Path.GetExtension(trimmed);
            if (!string.IsNullOrEmpty(extension) && !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Process name extension must be .exe when an extension is supplied.", nameof(processName));
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(trimmed);
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            {
                throw new ArgumentException("Process name must include a valid executable file name.", nameof(processName));
            }

            return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                ? $"{fileNameWithoutExtension}.exe"
                : $"{trimmed}.exe";
        }

        private static string EscapeWqlStringLiteral(string value)
            => value.Replace("'", "''", StringComparison.Ordinal);

        public void Dispose()
        {
            foreach (var w in _startWatchers) { w.Stop(); w.Dispose(); }
            foreach (var w in _stopWatchers)  { w.Stop(); w.Dispose(); }
        }
    }
}
