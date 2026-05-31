using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Xml.Linq;

namespace FsBootStrapper.Core.Infrastructure;

/// <summary>
/// Reads the embedded Win32 manifest of a PE executable to determine whether
/// it declares a UAC execution level of requireAdministrator or highestAvailable.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ExecutableManifestReader
{
    private const uint LoadLibraryAsDatafile    = 0x00000002;
    private const uint LoadLibraryAsImageResource = 0x00000020;
    private static readonly nint RtManifest = (nint)24;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint LoadLibraryEx(string lpFileName, nint hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(nint hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint FindResource(nint hModule, nint lpName, nint lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint LoadResource(nint hModule, nint hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint LockResource(nint hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(nint hModule, nint hResInfo);

    /// <summary>
    /// Returns <see langword="true"/> if the executable at <paramref name="executablePath"/>
    /// declares <c>requireAdministrator</c> or <c>highestAvailable</c> in its manifest.
    /// Returns <see langword="false"/> on any error or if no manifest is found.
    /// </summary>
    public static bool RequiresElevation(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return false;

        // Check for an external side-by-side manifest first (.exe.manifest)
        var externalManifest = executablePath + ".manifest";
        if (File.Exists(externalManifest))
        {
            try { return XmlRequiresElevation(File.ReadAllText(externalManifest)); }
            catch { /* fall through to embedded */ }
        }

        // Read the embedded RT_MANIFEST resource (resource ID 1 = CREATEPROCESS_MANIFEST_RESOURCE_ID)
        nint hModule = LoadLibraryEx(executablePath, nint.Zero,
            LoadLibraryAsDatafile | LoadLibraryAsImageResource);

        if (hModule == nint.Zero)
            return false;

        try
        {
            nint hResInfo = FindResource(hModule, (nint)1, RtManifest);
            if (hResInfo == nint.Zero) return false;

            nint hResData = LoadResource(hModule, hResInfo);
            if (hResData == nint.Zero) return false;

            nint pData = LockResource(hResData);
            if (pData == nint.Zero) return false;

            uint size = SizeofResource(hModule, hResInfo);
            if (size == 0) return false;

            var bytes = new byte[size];
            Marshal.Copy(pData, bytes, 0, (int)size);

            // Strip UTF-8 BOM if present
            var xml = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
            return XmlRequiresElevation(xml);
        }
        catch
        {
            return false;
        }
        finally
        {
            FreeLibrary(hModule);
        }
    }

    private static bool XmlRequiresElevation(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var el = doc.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "requestedExecutionLevel");

            var level = el?.Attribute("level")?.Value;
            return level is "requireAdministrator" or "highestAvailable";
        }
        catch
        {
            return false;
        }
    }
}
