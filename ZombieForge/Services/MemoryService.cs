using System;
using System.Runtime.InteropServices;

namespace ZombieForge.Services
{
    /// <summary>
    /// Provides low-level process-memory read helpers for game stat polling.
    /// </summary>
    public static class MemoryService
    {
        private const int PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, nuint nSize, out nuint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Opens a process handle with read permissions for memory access.
        /// </summary>
        /// <param name="processId">The target process identifier.</param>
        /// <returns>A process handle, or <see cref="IntPtr.Zero"/> when opening fails.</returns>
        public static IntPtr OpenGameProcess(int processId) =>
            OpenProcess(PROCESS_VM_READ, false, processId);

        /// <summary>
        /// Closes a process handle previously returned by <see cref="OpenGameProcess"/>.
        /// </summary>
        /// <param name="handle">The process handle to close.</param>
        public static void CloseGameProcess(IntPtr handle) =>
            CloseHandle(handle);

        /// <summary>
        /// Reads a 32-bit signed integer from a target process address.
        /// </summary>
        /// <param name="processHandle">A handle to the target process.</param>
        /// <param name="address">The absolute address to read.</param>
        /// <param name="value">When this method returns, contains the value that was read. This parameter is treated as uninitialized.</param>
        /// <param name="win32Error">When this method returns, contains the last Win32 error code when the read fails; otherwise, <c>0</c>. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if the read succeeds; otherwise, <see langword="false" />.</returns>
        public static bool TryReadInt32(IntPtr processHandle, long address, out int value, out int win32Error)
        {
            byte[] buffer = new byte[4];
            bool success = ReadProcessMemory(processHandle, (IntPtr)address, buffer, (nuint)buffer.Length, out _);
            if (!success)
            {
                value = default;
                win32Error = Marshal.GetLastWin32Error();
                return false;
            }

            value = BitConverter.ToInt32(buffer, 0);
            win32Error = 0;
            return true;
        }
    }
}
