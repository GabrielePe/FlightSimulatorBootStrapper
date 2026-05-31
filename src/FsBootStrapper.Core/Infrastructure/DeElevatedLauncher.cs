using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace FsBootStrapper.Core.Infrastructure;

/// <summary>
/// Launches a process at Medium Integrity Level (de-elevated) from an elevated host process,
/// by borrowing the token of the shell (explorer.exe).
/// This allows FSBS to run as Administrator while still launching non-elevated add-ons
/// at the same integrity level as MSFS (Xbox / Store), enabling inter-process communication.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DeElevatedLauncher
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public nint hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    private const uint NORMAL_PRIORITY_CLASS = 0x00000020;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const int TOKEN_DUPLICATE = 0x0002;
    private const int TOKEN_QUERY = 0x0008;
    private const int TOKEN_ASSIGN_PRIMARY = 0x0001;
    private const int TOKEN_ADJUST_DEFAULT = 0x0080;
    private const int TOKEN_ADJUST_SESSIONID = 0x0100;
    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        nint hExistingToken, uint dwDesiredAccess, nint lpTokenAttributes,
        int impersonationLevel, int tokenType, out nint phNewToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithTokenW(
        nint hToken, uint dwLogonFlags, string? lpApplicationName, string lpCommandLine,
        uint dwCreationFlags, nint lpEnvironment, string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    /// <summary>
    /// Starts <paramref name="fileName"/> at Medium IL using explorer's token.
    /// Returns the launched <see cref="Process"/>, or throws on failure.
    /// </summary>
    public static Process Start(string fileName, string arguments, string workingDirectory)
    {
        // Find explorer running in the same session
        var explorer = Process.GetProcessesByName("explorer")
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Cannot de-elevate: explorer.exe not found.");

        nint explorerToken = nint.Zero;
        nint duplicatedToken = nint.Zero;

        try
        {
            if (!OpenProcessToken(explorer.Handle, TOKEN_DUPLICATE | TOKEN_QUERY, out explorerToken))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed");

            uint tokenAccess = TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY |
                               TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;

            if (!DuplicateTokenEx(explorerToken, tokenAccess, nint.Zero,
                    SecurityImpersonation, TokenPrimary, out duplicatedToken))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx failed");

            // Build command line (CreateProcessWithTokenW requires a mutable string)
            var commandLine = string.IsNullOrEmpty(arguments)
                ? $"\"{fileName}\""
                : $"\"{fileName}\" {arguments}";

            var si = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };

            if (!CreateProcessWithTokenW(
                    duplicatedToken,
                    0,                          // dwLogonFlags
                    null,                       // lpApplicationName (use commandLine instead)
                    commandLine,
                    CREATE_UNICODE_ENVIRONMENT,
                    nint.Zero,
                    string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory,
                    ref si,
                    out var pi))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"CreateProcessWithTokenW failed for '{fileName}'");
            }

            CloseHandle(pi.hThread);
            // Wrap the raw handle in a Process object
            var process = Process.GetProcessById((int)pi.dwProcessId);
            CloseHandle(pi.hProcess);
            return process;
        }
        finally
        {
            if (explorerToken != nint.Zero) CloseHandle(explorerToken);
            if (duplicatedToken != nint.Zero) CloseHandle(duplicatedToken);
        }
    }
}
