using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FsBootStrapper.Core.Infrastructure;

/// <summary>
/// Signals process shutdown the same way Windows does when the user clicks a window's
/// title-bar "X" (WM_SYSCOMMAND/SC_CLOSE), but applied to every top-level window owned
/// by the process - including hidden/tray-only windows that Process.CloseMainWindow()
/// cannot see because it only targets the single window .NET guesses is the "main" one.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeWindowCloser
{
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    private const uint WM_SYSCOMMAND = 0x0112;
    private const nint SC_CLOSE = 0xF060;

    /// <summary>
    /// Posts a close signal to every top-level window belonging to <paramref name="processId"/>.
    /// Returns the number of windows signaled (0 means the process has nothing to close
    /// gracefully, e.g. a console-only or already-exited process).
    /// </summary>
    public static int CloseAllWindows(int processId)
    {
        var handles = new List<nint>();

        // EnumWindows walks every top-level window regardless of visibility, so tray-only
        // apps (hidden main window, no taskbar entry) are still reachable here.
        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == (uint)processId)
            {
                handles.Add(hWnd);
            }
            return true;
        }, nint.Zero);

        foreach (var hWnd in handles)
        {
            PostMessage(hWnd, WM_SYSCOMMAND, SC_CLOSE, nint.Zero);
        }

        return handles.Count;
    }
}
