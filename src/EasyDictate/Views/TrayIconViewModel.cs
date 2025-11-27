using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using EasyDictate.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace EasyDictate.Views;

/// <summary>
/// ViewModel for the system tray icon
/// </summary>
public class TrayIconViewModel : INotifyPropertyChanged
{
    private TaskbarIcon? _trayIcon;
    private string _tooltipText = "EasyDictate - Ready";
    private DictationState _currentState = DictationState.Idle;
    private Icon? _appIcon;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TooltipText
    {
        get => _tooltipText;
        set
        {
            _tooltipText = value;
            OnPropertyChanged();
        }
    }

    public ICommand ShowSettingsCommand => new RelayCommand(_ => App.ShowSettings());
    public ICommand ExitCommand => new RelayCommand(_ => Application.Current.Shutdown());

    public TrayIconViewModel()
    {
        // Load the icon from resources
        LoadAppIcon();
        
        // Subscribe to coordinator state changes
        if (App.Coordinator != null)
        {
            App.Coordinator.StateChanged += OnStateChanged;
        }
    }

    private void LoadAppIcon()
    {
        try
        {
            // Load icon from embedded resource
            var resourceUri = new Uri("pack://application:,,,/EasyDictate;component/Resources/icon.ico", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                _appIcon = new Icon(streamInfo.Stream);
            }
        }
        catch
        {
            // Fallback - icon will be generated
            _appIcon = null;
        }
    }

    private void OnStateChanged(object? sender, DictationState state)
    {
        _currentState = state;
        
        var hotkeyText = SettingsService.GetHotkeyDisplayName(
            App.Settings.Current.HotkeyModifiers,
            App.Settings.Current.HotkeyKey);
        
        TooltipText = state switch
        {
            DictationState.Idle => $"EasyDictate\nHold {hotkeyText} to dictate",
            DictationState.Listening => "EasyDictate - Listening...",
            DictationState.Transcribing => "EasyDictate - Transcribing...",
            DictationState.Error => "EasyDictate - Error",
            _ => "EasyDictate"
        };

        UpdateIcon(_trayIcon);
    }

    public void UpdateIcon(TaskbarIcon? icon)
    {
        _trayIcon = icon;
        if (_trayIcon == null) return;

        // Use loaded icon if available, otherwise generate one
        if (_appIcon != null && _currentState == DictationState.Idle)
        {
            _trayIcon.Icon = _appIcon;
        }
        else
        {
            // Generate state-specific icon for listening/transcribing/error
            var iconBitmap = GenerateStateIcon(_currentState);
            _trayIcon.Icon = Icon.FromHandle(iconBitmap.GetHicon());
        }
    }

    private Bitmap GenerateStateIcon(DictationState state)
    {
        var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background with rounded corners
        var bgColor = state switch
        {
            DictationState.Listening => Color.FromArgb(239, 68, 68),    // Red
            DictationState.Transcribing => Color.FromArgb(245, 158, 11), // Amber
            DictationState.Error => Color.FromArgb(239, 68, 68),         // Red
            _ => Color.FromArgb(79, 70, 229)                             // Purple (#4f46e5)
        };

        using var bgBrush = new SolidBrush(bgColor);
        
        // Draw rounded rectangle
        var rect = new Rectangle(0, 0, 31, 31);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        int radius = 6;
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(bgBrush, path);

        // Draw microphone
        using var whitePen = new Pen(Color.White, 2);
        
        // Mic body (rounded rectangle)
        g.DrawRoundedRectangle(whitePen, 12, 6, 8, 12, 4);
        
        // Mic arc
        g.DrawArc(whitePen, 9, 10, 14, 12, 0, 180);
        
        // Stand
        g.DrawLine(whitePen, 16, 22, 16, 26);

        return bitmap;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Extension methods for Graphics
/// </summary>
public static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }
}

/// <summary>
/// Simple relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
