namespace FsBootStrapper.Services;

public sealed class WindowStateService : IWindowStateService
{
    public event EventHandler? MinimizeToTrayRequested;

    public void RequestMinimizeToTray() => MinimizeToTrayRequested?.Invoke(this, EventArgs.Empty);
}
