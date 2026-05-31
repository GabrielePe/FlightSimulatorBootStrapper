using FsBootStrapper.Core.Models;
using System.Xml.Linq;

namespace FsBootStrapper.Core.Services.Import;

/// <summary>
/// Parses an MSFS exe.xml file and returns <see cref="LaunchItem"/> instances
/// for each enabled add-on. All entries from exe.xml are treated as
/// <see cref="LaunchPhase.AfterMsfs"/> because that is how exe.xml works —
/// MSFS launches them right after it starts.
/// </summary>
public static class ExeXmlImporter
{
    /// <summary>
    /// Parses the given exe.xml file and returns one <see cref="LaunchItem"/>
    /// per <c>Launch.Addon</c> entry that is not disabled.
    /// </summary>
    /// <param name="xmlPath">Full path to the exe.xml file.</param>
    /// <returns>Ordered list of <see cref="LaunchItem"/> ready to be added to a profile.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file does not look like a valid MSFS exe.xml.
    /// </exception>
    public static IReadOnlyList<LaunchItem> Import(string xmlPath)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(xmlPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot read '{xmlPath}': {ex.Message}", ex);
        }

        var root = doc.Root;
        if (root is null || root.Name.LocalName != "SimBase.Document")
            throw new InvalidOperationException(
                "The selected file does not appear to be a valid MSFS exe.xml (root element is not SimBase.Document).");

        var items = new List<LaunchItem>();
        int order = 0;

        foreach (var addon in root.Elements("Launch.Addon"))
        {
            // Skip entries that are explicitly disabled
            var disabledValue = addon.Element("Disabled")?.Value ?? "false";
            if (string.Equals(disabledValue, "true", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = addon.Element("Path")?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var name = addon.Element("Name")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = System.IO.Path.GetFileNameWithoutExtension(path);

            var cmdLine = addon.Element("CommandLine")?.Value?.Trim();

            items.Add(new LaunchItem
            {
                DisplayName = name!,
                ExecutablePath = path,
                Arguments = string.IsNullOrWhiteSpace(cmdLine) ? null : cmdLine,
                Phase = LaunchPhase.AfterMsfs,
                SortOrder = order++,
                AutoCloseSupported = false,
                CloseMethod = CloseMethod.GracefulClose
            });
        }

        return items;
    }

    /// <summary>
    /// Creates a timestamped backup of <paramref name="xmlPath"/> and then removes
    /// all <c>Launch.Addon</c> nodes from the file, leaving the rest of the
    /// document intact so MSFS still starts without errors.
    /// </summary>
    /// <param name="xmlPath">Full path to the exe.xml file to clean.</param>
    /// <returns>Full path of the backup file that was created.</returns>
    /// <exception cref="InvalidOperationException">Thrown on I/O or parse errors.</exception>
    public static string BackupAndClean(string xmlPath)
    {
        // Build a backup name like "fsbs_20260531-1610_exe.xml.bak" in the same folder
        var dir = System.IO.Path.GetDirectoryName(xmlPath)!;
        var fileName = System.IO.Path.GetFileName(xmlPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var backupPath = System.IO.Path.Combine(dir, $"fsbs_{timestamp}_{fileName}.bak");

        try
        {
            System.IO.File.Copy(xmlPath, backupPath, overwrite: false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not create backup at '{backupPath}': {ex.Message}", ex);
        }

        // Load, strip Launch.Addon elements, save back
        XDocument doc;
        try
        {
            doc = XDocument.Load(xmlPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot parse '{xmlPath}': {ex.Message}", ex);
        }

        doc.Root?.Elements("Launch.Addon").Remove();

        try
        {
            // Preserve the original XML declaration (utf-8, version 1.0)
            var settings = new System.Xml.XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                OmitXmlDeclaration = false
            };
            using var writer = System.Xml.XmlWriter.Create(xmlPath, settings);
            doc.Save(writer);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not write cleaned file to '{xmlPath}': {ex.Message}", ex);
        }

        return backupPath;
    }
}
