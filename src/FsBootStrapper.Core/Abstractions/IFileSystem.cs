namespace FsBootStrapper.Core.Abstractions;

/// <summary>
/// Abstraction over file-system I/O to allow deterministic unit testing
/// without touching the real file system.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void EnsureDirectoryExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void DeleteFile(string path);
    IEnumerable<string> GetFiles(string directory, string searchPattern);
}
