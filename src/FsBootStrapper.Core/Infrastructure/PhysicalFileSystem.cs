using FsBootStrapper.Core.Abstractions;

namespace FsBootStrapper.Core.Infrastructure;

/// <summary>Production implementation backed by System.IO.</summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string contents) =>
        File.WriteAllText(path, contents);

    public void DeleteFile(string path) => File.Delete(path);

    public IEnumerable<string> GetFiles(string directory, string searchPattern) =>
        Directory.GetFiles(directory, searchPattern);
}
