namespace FsBootStrapper.Core.Services.Notifications;

public enum NotificationSeverity { Info, Warning, Error }

public interface INotificationService
{
    void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info);
}
