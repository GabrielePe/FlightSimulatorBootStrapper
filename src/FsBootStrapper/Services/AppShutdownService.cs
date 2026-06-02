namespace FsBootStrapper.Services;

public interface IAppShutdownService
{
    bool ShutdownRequested { get; }
    void RequestShutdown();
}

public sealed class AppShutdownService : IAppShutdownService
{
    private int _shutdownRequested;

    public bool ShutdownRequested => _shutdownRequested != 0;

    public void RequestShutdown() => Interlocked.Exchange(ref _shutdownRequested, 1);
}