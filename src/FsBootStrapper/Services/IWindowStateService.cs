namespace FsBootStrapper.Services;

public interface IWindowStateService
{
    event EventHandler? MinimizeToTrayRequested;
    void RequestMinimizeToTray();
}
