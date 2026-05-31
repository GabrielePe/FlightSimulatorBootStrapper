using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace FsBootStrapper.Core.Models;

/// <summary>A named launch configuration containing an ordered list of items.</summary>
public sealed class AppProfile : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public Guid Id { get; set; } = Guid.NewGuid();

    private string _name = string.Empty;
    [Required, MaxLength(100)]
    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; Notify(); } }
    }

    private string? _description;
    [MaxLength(500)]
    public string? Description
    {
        get => _description;
        set { if (_description != value) { _description = value; Notify(); } }
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ordered list of add-on entries.
    /// Uses ObservableCollection so DataGrid and ListBox update on add/remove.
    /// Within each <see cref="LaunchPhase"/> the items are sorted by <see cref="LaunchItem.SortOrder"/>.
    /// </summary>
    public ObservableCollection<LaunchItem> Items { get; set; } = [];

    /// <summary>Returns items filtered and sorted for a given phase.</summary>
    public IEnumerable<LaunchItem> GetItemsForPhase(LaunchPhase phase) =>
        Items.Where(i => i.Phase == phase).OrderBy(i => i.SortOrder);
}
