using System;
using System.Runtime.InteropServices;

namespace ZombieForge.Services
{
    public static class MemoryService
    {
        private const int PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, nuint nSize, out nuint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public static IntPtr OpenGameProcess(int processId) =>
            OpenProcess(PROCESS_VM_READ, false, processId);

        public static void CloseGameProcess(IntPtr handle) =>
            CloseHandle(handle);

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
