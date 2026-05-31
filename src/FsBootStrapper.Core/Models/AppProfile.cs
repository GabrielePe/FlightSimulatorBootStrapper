using System.ComponentModel.DataAnnotations;

namespace FsBootStrapper.Core.Models;

/// <summary>A named launch configuration containing an ordered list of items.</summary>
public sealed class AppProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ordered list of add-on entries.
    /// Within each <see cref="LaunchPhase"/> the items are sorted by <see cref="LaunchItem.SortOrder"/>.
    /// </summary>
    public List<LaunchItem> Items { get; set; } = [];

    /// <summary>Returns items filtered and sorted for a given phase.</summary>
    public IEnumerable<LaunchItem> GetItemsForPhase(LaunchPhase phase) =>
        Items.Where(i => i.Phase == phase).OrderBy(i => i.SortOrder);
}
