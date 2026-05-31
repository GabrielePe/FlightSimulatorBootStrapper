using System.Windows.Controls;

namespace FsBootStrapper.Services;

/// <summary>Simple page navigation contract driven by the content Frame.</summary>
public interface ISimpleNavigationService
{
    void Navigate(Type pageType);
    void SetFrame(Frame frame);
}

/// <summary>Implementation that drives a WPF <see cref="Frame"/>.</summary>
public sealed class NavigationService : ISimpleNavigationService
{
    private Frame? _frame;
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    public void SetFrame(Frame frame) => _frame = frame;

    public void Navigate(Type pageType)
    {
        if (_frame is null) return;
        var page = _services.GetService(pageType);
        if (page is not null)
            _frame.Navigate(page);
    }
}
