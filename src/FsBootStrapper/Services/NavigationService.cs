using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace FsBootStrapper.Services;

/// <summary>Simple page navigation contract.</summary>
public interface ISimpleNavigationService
{
    void Navigate(Type pageType);
    void SetNavigationView(NavigationView nav);
}

/// <summary>Delegates navigation to the Wpf.Ui <see cref="NavigationView"/>.</summary>
public sealed class NavigationService : ISimpleNavigationService
{
    private NavigationView? _navigationView;

    public void SetNavigationView(NavigationView nav) => _navigationView = nav;

    public void Navigate(Type pageType) =>
        _navigationView?.Navigate(pageType, null);
}
