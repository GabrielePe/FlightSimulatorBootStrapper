namespace FsBootStrapper.Core.Abstractions;

/// <summary>
/// Abstraction over Windows registry reads to allow testing without
/// a real registry.
/// </summary>
public interface IRegistryReader
{
    string? GetLocalMachineValue(string subKey, string valueName);
}
