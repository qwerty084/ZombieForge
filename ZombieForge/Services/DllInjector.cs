using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ZombieForge.Services
{
    public static class DllInjector
    {
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int MEM_COMMIT = 0x1000;
        private const int MEM_RESERVE = 0x2000;
        private const int MEM_RELEASE = 0x8000;
        private const int PAGE_READWRITE = 0x04;
        private const uint INFINITE = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out nuint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetModuleHandleA(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        /// <summary>
        /// Checks whether the target process has the same architecture (bitness)
        /// as the current process. Injection requires matching architectures because
        /// LoadLibraryW is resolved in the caller's address space.
        /// </summary>
        private static bool ArchitectureMatchesTarget(IntPtr hTargetProcess, ILogger logger)
        {
            bool callerIs64 = Environment.Is64BitProcess;

            if (!Environment.Is64BitOperatingSystem)
            {
                // 32-bit OS: everything is 32-bit, always matches.
                return true;
            }

            if (!IsWow64Process(hTargetProcess, out bool targetIsWow64))
            {
                logger.LogWarning("IsWow64Process failed, Win32Error={Error}", Marshal.GetLastWin32Error());
                return false;
            }

            // On 64-bit OS: WOW64 process = 32-bit, non-WOW64 = 64-bit.
            bool targetIs64 = !targetIsWow64;

            if (callerIs64 != targetIs64)
            {
                logger.LogWarning(
                    "Architecture mismatch: ZombieForge is {CallerBits}-bit but target process is {TargetBits}-bit. " +
                    "DLL injection requires matching architectures. Build ZombieForge as {SuggestedArchitecture} to inject into this target process",
                    callerIs64 ? 64 : 32,
                    targetIs64 ? 64 : 32,
                    targetIs64 ? "x64" : "x86");
                return false;
            }

            return true;
        }

        public static bool Inject(int processId, string dllPath, ILogger logger)
        {
            string fullPath = Path.GetFullPath(dllPath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning("DLL not found at {Path}", fullPath);
                return false;
            }

            IntPtr hProcess = OpenProcess(
                PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION,
                false, processId);

            if (hProcess == IntPtr.Zero)
            {
                logger.LogWarning("OpenProcess failed for PID={Pid}, Win32Error={Error}",
                    processId, Marshal.GetLastWin32Error());
                return false;
            }

            if (!ArchitectureMatchesTarget(hProcess, logger))
            {
                CloseHandle(hProcess);
                return false;
            }

            try
            {
                return InjectInternal(hProcess, processId, fullPath, logger);
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        private static bool InjectInternal(IntPtr hProcess, int processId, string dllPath, ILogger logger)
        {
            // Use LoadLibraryW so Unicode/non-ASCII install paths work correctly.
            // This export is resolved from the injector's kernel32 and passed to CreateRemoteThread,
            // which is only safe because ArchitectureMatchesTarget already rejected cross-bitness injection.
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryW");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                logger.LogWarning("Failed to resolve LoadLibraryW");
                return false;
            }

            // Encode as UTF-16LE (null terminator included) to match the W API.
            byte[] pathBytes = Encoding.Unicode.GetBytes(dllPath + '\0');
            uint pathSize = (uint)pathBytes.Length;

            IntPtr remoteMemory = VirtualAllocEx(hProcess, IntPtr.Zero, pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (remoteMemory == IntPtr.Zero)
            {
                logger.LogWarning("VirtualAllocEx failed, Win32Error={Error}", Marshal.GetLastWin32Error());
                return false;
            }

            try
            {
                if (!WriteProcessMemory(hProcess, remoteMemory, pathBytes, pathSize, out _))
                {
                    logger.LogWarning("WriteProcessMemory failed, Win32Error={Error}", Marshal.GetLastWin32Error());
                    return false;
                }

                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, remoteMemory, 0, out _);
                if (hThread == IntPtr.Zero)
                {
                    logger.LogWarning("CreateRemoteThread failed, Win32Error={Error}", Marshal.GetLastWin32Error());
                    return false;
                }

                WaitForSingleObject(hThread, INFINITE);
                CloseHandle(hThread);

                logger.LogInformation("DLL injected into PID={Pid}: {Path}",
                    processId, dllPath);
                return true;
            }
            finally
            {
                VirtualFreeEx(hProcess, remoteMemory, 0, MEM_RELEASE);
            }
        }
    }
}
