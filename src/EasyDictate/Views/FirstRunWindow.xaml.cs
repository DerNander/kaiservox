using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EasyDictate.Models;
using EasyDictate.Services;

namespace EasyDictate.Views;

/// <summary>
/// First-run setup wizard
/// </summary>
public partial class FirstRunWindow : Window
{
    private int _currentStep = 1;
    private const int TotalSteps = 5;
    private bool _isCapturingHotkey;
    private CancellationTokenSource? _downloadCts;

    private readonly Ellipse[] _dots;
    private readonly StackPanel[] _panels;

    public FirstRunWindow()
    {
        InitializeComponent();

        _dots = new[] { Dot1, Dot2, Dot3, Dot4, Dot5 };
        _panels = new[] { Step1Panel, Step2Panel, Step3Panel, Step4Panel, Step5Panel };

        LoadMicrophones();
        UpdateHotkeyDisplay();
    }

    private void LoadMicrophones()
    {
        var devices = AudioCaptureService.GetMicrophoneDevices();
        MicrophoneComboBox.Items.Clear();

        foreach (var (id, name) in devices)
        {
            MicrophoneComboBox.Items.Add(new MicrophoneItem(id, name));
        }

        if (MicrophoneComboBox.Items.Count > 0)
        {
            MicrophoneComboBox.SelectedIndex = 0;
        }
    }

    private void UpdateHotkeyDisplay()
    {
        HotkeyDisplay.Text = SettingsService.GetHotkeyDisplayName(
            App.Settings.Current.HotkeyModifiers,
            App.Settings.Current.HotkeyKey);
    }

    private void UpdateStep()
    {
        // Update panels visibility
        for (int i = 0; i < _panels.Length; i++)
        {
            _panels[i].Visibility = (i == _currentStep - 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Update dots
        var primaryBrush = (SolidColorBrush)FindResource("PrimaryBrush");
        var inactiveBrush = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x5A));

        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Fill = (i < _currentStep) ? primaryBrush : inactiveBrush;
        }

        // Update title and description
        (StepTitle.Text, StepDescription.Text) = _currentStep switch
        {
            1 => ("Welcome to EasyDictate", "Let's get you set up in just a few steps"),
            2 => ("Select Microphone", "Choose the microphone you'll use for dictation"),
            3 => ("Configure Hotkey", "Set up your push-to-talk keyboard shortcut"),
            4 => ("Download Model", "Get the speech recognition model"),
            5 => ("Setup Complete", "You're ready to start dictating!"),
            _ => ("", "")
        };

        // Update buttons
        BackButton.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;

        NextButton.Content = _currentStep switch
        {
            1 => "Get Started",
            4 => "Download",
            5 => "Finish",
            _ => "Continue"
        };

        // Update final step text
        if (_currentStep == 5)
        {
            FinalHotkeyText.Text = $"Press {SettingsService.GetHotkeyDisplayName(App.Settings.Current.HotkeyModifiers, App.Settings.Current.HotkeyKey)} to start dictating";
        }
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 2)
        {
            // Save microphone selection
            if (MicrophoneComboBox.SelectedItem is MicrophoneItem item)
            {
                App.Settings.Current.MicrophoneDeviceId = item.Id;
            }
        }
        else if (_currentStep == 4)
        {
            // Download model
            if (!App.ModelManager.IsModelDownloaded())
            {
                await DownloadModelAsync();
                return; // Will advance step after download
            }
        }
        else if (_currentStep == 5)
        {
            // Finish setup
            App.Settings.Current.IsFirstRun = false;
            await App.Settings.SaveAsync();
            Close();
            return;
        }

        _currentStep++;
        UpdateStep();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            UpdateStep();
        }
    }

    private async Task DownloadModelAsync()
    {
        NextButton.IsEnabled = false;
        BackButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        DownloadStatusText.Text = "Starting download...";

        _downloadCts = new CancellationTokenSource();

        App.ModelManager.DownloadProgress += OnDownloadProgress;

        try
        {
            await App.ModelManager.DownloadModelAsync(_downloadCts.Token);
            
            DownloadStatusText.Text = "Loading model...";
            await App.Transcription.InitializeAsync();

            DownloadStatusText.Text = "Complete!";
            
            // Advance to next step
            _currentStep++;
            UpdateStep();
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText.Text = "Download cancelled";
        }
        catch (Exception ex)
        {
            DownloadStatusText.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to download model: {ex.Message}", "Download Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            App.ModelManager.DownloadProgress -= OnDownloadProgress;
            NextButton.IsEnabled = true;
            BackButton.IsEnabled = true;
            _downloadCts = null;
        }
    }

    private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            DownloadProgress.Value = e.PercentComplete;
            var mbDownloaded = e.BytesDownloaded / 1024.0 / 1024.0;
            var mbTotal = e.TotalBytes / 1024.0 / 1024.0;
            DownloadStatusText.Text = $"Downloading... {mbDownloaded:F1} / {mbTotal:F1} MB ({e.PercentComplete:F0}%)";
        });
    }

    private void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturingHotkey)
        {
            CancelHotkeyCapture();
            return;
        }

        _isCapturingHotkey = true;
        HotkeyDisplay.Text = "Press a key combination...";
        ChangeHotkeyButton.Content = "Cancel";

        PreviewKeyDown += OnHotkeyCapture;
    }

    private void OnHotkeyCapture(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore modifier-only keys
        if (key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Build modifiers
        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            modifiers |= HotkeyModifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            modifiers |= HotkeyModifiers.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            modifiers |= HotkeyModifiers.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            modifiers |= HotkeyModifiers.Win;

        // Require at least one modifier
        if (modifiers == HotkeyModifiers.None)
        {
            HotkeyDisplay.Text = "Must include Alt, Ctrl, Shift, or Win";
            return;
        }

        var vk = KeyInterop.VirtualKeyFromKey(key);

        App.Settings.Current.HotkeyModifiers = modifiers;
        App.Settings.Current.HotkeyKey = vk;

        HotkeyDisplay.Text = SettingsService.GetHotkeyDisplayName(modifiers, vk);
        FinishHotkeyCapture();
    }

    private void CancelHotkeyCapture()
    {
        FinishHotkeyCapture();
        UpdateHotkeyDisplay();
    }

    private void FinishHotkeyCapture()
    {
        _isCapturingHotkey = false;
        ChangeHotkeyButton.Content = "Click to change";
        PreviewKeyDown -= OnHotkeyCapture;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _downloadCts?.Cancel();
        base.OnClosing(e);
    }
}

