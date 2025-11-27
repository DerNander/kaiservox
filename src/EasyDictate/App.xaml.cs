using System.Windows;
using EasyDictate.Services;
using EasyDictate.Views;
using Hardcodet.Wpf.TaskbarNotification;

namespace EasyDictate;

/// <summary>
/// Main application class for EasyDictate
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private TrayIconViewModel? _trayViewModel;
    
    // Services
    public static SettingsService Settings { get; private set; } = null!;
    public static HotkeyService Hotkey { get; private set; } = null!;
    public static AudioCaptureService AudioCapture { get; private set; } = null!;
    public static TranscriptionService Transcription { get; private set; } = null!;
    public static OutputService Output { get; private set; } = null!;
    public static ModelManager ModelManager { get; private set; } = null!;
    public static DictationCoordinator Coordinator { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers to prevent silent crashes
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Unhandled exception:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "EasyDictate Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"UI exception:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "EasyDictate Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // Prevent crash
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            MessageBox.Show($"Task exception:\n\n{args.Exception.Message}",
                "EasyDictate Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.SetObserved();
        };

        // Initialize services
        Settings = new SettingsService();
        await Settings.LoadAsync();

        ModelManager = new ModelManager(Settings);
        AudioCapture = new AudioCaptureService(Settings);
        Transcription = new TranscriptionService(ModelManager);
        Output = new OutputService(Settings);
        Hotkey = new HotkeyService();
        Coordinator = new DictationCoordinator(AudioCapture, Transcription, Output);

        // Check if first run
        if (Settings.Current.IsFirstRun)
        {
            ShowFirstRunWizard();
        }
        else
        {
            await InitializeNormalStartup();
        }
    }

    private void ShowFirstRunWizard()
    {
        var wizard = new FirstRunWindow();
        wizard.Closed += async (s, e) =>
        {
            if (Settings.Current.IsFirstRun)
            {
                // User closed without completing - exit
                Shutdown();
                return;
            }
            await InitializeNormalStartup();
        };
        wizard.Show();
    }

    private async Task InitializeNormalStartup()
    {
        // Load model if available
        if (ModelManager.IsModelDownloaded())
        {
            try
            {
                await Transcription.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load transcription model: {ex.Message}\n\nPlease check Settings to re-download the model.",
                    "EasyDictate", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Setup tray icon programmatically
        CreateTrayIcon();

        // Register hotkey
        RegisterHotkey();
    }

    private void CreateTrayIcon()
    {
        var hotkeyText = SettingsService.GetHotkeyDisplayName(
            Settings.Current.HotkeyModifiers, 
            Settings.Current.HotkeyKey);
        
        _trayViewModel = new TrayIconViewModel();
        _trayIcon = new TaskbarIcon
        {
            DataContext = _trayViewModel,
            ToolTipText = $"EasyDictate\nHold {hotkeyText} to dictate",
            ContextMenu = CreateContextMenu()
        };
        _trayViewModel.UpdateIcon(_trayIcon);
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var hotkeyText = SettingsService.GetHotkeyDisplayName(
            Settings.Current.HotkeyModifiers, 
            Settings.Current.HotkeyKey);
        
        var menu = new System.Windows.Controls.ContextMenu();
        
        // Show hotkey info at the top
        var hotkeyItem = new System.Windows.Controls.MenuItem 
        { 
            Header = $"Hold {hotkeyText} to dictate",
            IsEnabled = false,
            FontWeight = System.Windows.FontWeights.SemiBold
        };
        menu.Items.Add(hotkeyItem);
        
        menu.Items.Add(new System.Windows.Controls.Separator());
        
        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (s, e) => ShowSettings();
        menu.Items.Add(settingsItem);
        
        menu.Items.Add(new System.Windows.Controls.Separator());
        
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void RegisterHotkey()
    {
        var settings = Settings.Current;
        Hotkey.RegisterPushToTalk(
            settings.HotkeyModifiers,
            settings.HotkeyKey,
            onKeyDown: () => Coordinator.StartDictation(),
            onKeyUp: () => Coordinator.StopDictation()
        );
    }

    public static void ShowSettings()
    {
        var existing = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var settings = new SettingsWindow();
        settings.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        Hotkey?.Dispose();
        AudioCapture?.Dispose();
        Transcription?.Dispose();
        base.OnExit(e);
    }
}

