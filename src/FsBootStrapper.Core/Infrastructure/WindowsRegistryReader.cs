using FsBootStrapper.Core.Abstractions;
using Microsoft.Win32;

namespace FsBootStrapper.Core.Infrastructure;

/// <summary>Production implementation backed by the real Windows Registry.</summary>
public sealed class WindowsRegistryReader : IRegistryReader
{
    public string? GetLocalMachineValue(string subKey, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: false);
        return key?.GetValue(valueName)?.ToString();
    }
}
