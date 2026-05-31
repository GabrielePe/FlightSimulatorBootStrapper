using FsBootStrapper.Core.Services.Notifications;
using System.Windows;

namespace FsBootStrapper.Services;

/// <summary>
/// Delivers notifications via the system tray balloon tooltip.
/// Falls back to a MessageBox if the tray icon is unavailable.
/// </summary>
public sealed class TrayNotificationService : INotificationService
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public void SetTrayIcon(System.Windows.Forms.NotifyIcon icon)
    {
        _trayIcon = icon;
    }

    public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        var toolTipIcon = severity switch
        {
            NotificationSeverity.Warning => System.Windows.Forms.ToolTipIcon.Warning,
            NotificationSeverity.Error => System.Windows.Forms.ToolTipIcon.Error,
            _ => System.Windows.Forms.ToolTipIcon.Info
        };

        if (_trayIcon is not null)
        {
            _trayIcon.ShowBalloonTip(
                timeout: 4000,
                tipTitle: title,
                tipText: message,
                tipIcon: toolTipIcon);
        }
    }
}
