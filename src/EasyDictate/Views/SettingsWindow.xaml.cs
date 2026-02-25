using System.Windows;
using System.Windows.Input;
using EasyDictate.Models;
using EasyDictate.Services;

namespace EasyDictate.Views;

/// <summary>
/// Settings window for configuring EasyDictate
/// </summary>
public partial class SettingsWindow : Window
{
    private bool _isCapturingHotkey;
    private HotkeyModifiers _pendingModifiers;
    private int _pendingKey;
    private bool _isLoading = true; // Prevent saving during load

    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        _isLoading = false; // Now allow saving
    }

    private void LoadSettings()
    {
        var settings = App.Settings.Current;

        // Hotkey
        HotkeyDisplay.Text = SettingsService.GetHotkeyDisplayName(
            settings.HotkeyModifiers, 
            settings.HotkeyKey);

        // Microphone
        LoadMicrophones();

        // Output mode
        PasteRadio.IsChecked = settings.OutputMode == OutputMode.PasteToActiveWindow;
        ClipboardRadio.IsChecked = settings.OutputMode == OutputMode.CopyToClipboard;

        // General
        StartupCheckbox.IsChecked = settings.RunOnStartup;
        OverlayCheckbox.IsChecked = settings.ShowOverlay;

        // Models
        LoadModels();
        UpdateModelStatus();
    }

    private void LoadMicrophones()
    {
        var devices = AudioCaptureService.GetMicrophoneDevices();
        MicrophoneComboBox.Items.Clear();
        
        foreach (var (id, name) in devices)
        {
            MicrophoneComboBox.Items.Add(new MicrophoneItem(id, name));
        }

        // Select current device
        var currentId = App.Settings.Current.MicrophoneDeviceId ?? "default";
        for (int i = 0; i < MicrophoneComboBox.Items.Count; i++)
        {
            if (((MicrophoneItem)MicrophoneComboBox.Items[i]).Id == currentId)
            {
                MicrophoneComboBox.SelectedIndex = i;
                break;
            }
        }

        if (MicrophoneComboBox.SelectedIndex < 0 && MicrophoneComboBox.Items.Count > 0)
        {
            MicrophoneComboBox.SelectedIndex = 0;
        }
    }

    private void LoadModels()
    {
        var models = App.ModelManager.GetAvailableModels();
        ModelComboBox.Items.Clear();

        foreach (var model in models)
        {
            ModelComboBox.Items.Add(model);
        }

        if (ModelComboBox.Items.Count == 0)
        {
            ModelComboBox.Items.Add("ggml-base.en.bin");
        }

        var selected = App.Settings.Current.SelectedModelFile;
        if (string.IsNullOrWhiteSpace(selected))
        {
            selected = App.ModelManager.GetSelectedModelFile();
            App.Settings.Current.SelectedModelFile = selected;
        }

        ModelComboBox.SelectedItem = selected;

        if (ModelComboBox.SelectedItem == null && ModelComboBox.Items.Count > 0)
        {
            ModelComboBox.SelectedIndex = 0;
            App.Settings.Current.SelectedModelFile = ModelComboBox.SelectedItem?.ToString() ?? "ggml-base.en.bin";
        }
    }

    private void UpdateModelStatus()
    {
        var selected = App.Settings.Current.SelectedModelFile;
        if (string.IsNullOrWhiteSpace(selected))
        {
            selected = App.ModelManager.GetSelectedModelFile();
            App.Settings.Current.SelectedModelFile = selected;
        }

        ModelStatusText.Text = selected;

        if (App.ModelManager.IsModelDownloaded())
        {
            ModelSizeText.Text = $"{App.ModelManager.GetModelSizeDisplay()} - Downloaded";
            RedownloadButton.Content = "Re-download";
        }
        else
        {
            ModelSizeText.Text = "Not downloaded";
            RedownloadButton.Content = "Download base model";
        }
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
        
        // Temporarily unregister current hotkey
        App.Hotkey.Unregister();

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
        _pendingModifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            _pendingModifiers |= HotkeyModifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            _pendingModifiers |= HotkeyModifiers.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            _pendingModifiers |= HotkeyModifiers.Shift;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            _pendingModifiers |= HotkeyModifiers.Win;

        // Require at least one modifier
        if (_pendingModifiers == HotkeyModifiers.None)
        {
            HotkeyDisplay.Text = "Must include a modifier key (Alt, Ctrl, Shift, or Win)";
            return;
        }

        _pendingKey = KeyInterop.VirtualKeyFromKey(key);

        // Save and apply
        App.Settings.Current.HotkeyModifiers = _pendingModifiers;
        App.Settings.Current.HotkeyKey = _pendingKey;
        _ = App.Settings.SaveAsync();

        HotkeyDisplay.Text = SettingsService.GetHotkeyDisplayName(_pendingModifiers, _pendingKey);
        
        FinishHotkeyCapture();

        // Re-register with new hotkey
        try
        {
            App.Hotkey.RegisterPushToTalk(
                _pendingModifiers,
                _pendingKey,
                () => App.Coordinator.StartDictation(),
                () => App.Coordinator.StopDictation());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to register hotkey: {ex.Message}\nThe hotkey may be in use by another application.",
                "Hotkey Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelHotkeyCapture()
    {
        FinishHotkeyCapture();
        HotkeyDisplay.Text = SettingsService.GetHotkeyDisplayName(
            App.Settings.Current.HotkeyModifiers,
            App.Settings.Current.HotkeyKey);

        // Re-register original hotkey
        try
        {
            App.Hotkey.RegisterPushToTalk(
                App.Settings.Current.HotkeyModifiers,
                App.Settings.Current.HotkeyKey,
                () => App.Coordinator.StartDictation(),
                () => App.Coordinator.StopDictation());
        }
        catch { }
    }

    private void FinishHotkeyCapture()
    {
        _isCapturingHotkey = false;
        ChangeHotkeyButton.Content = "Change";
        PreviewKeyDown -= OnHotkeyCapture;
    }

    private void MicrophoneComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        if (MicrophoneComboBox.SelectedItem is MicrophoneItem item)
        {
            App.Settings.Current.MicrophoneDeviceId = item.Id;
            _ = App.Settings.SaveAsync();
        }
    }

    private async void ModelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        if (ModelComboBox.SelectedItem is not string selectedModel || string.IsNullOrWhiteSpace(selectedModel)) return;

        if (string.Equals(App.Settings.Current.SelectedModelFile, selectedModel, StringComparison.OrdinalIgnoreCase))
        {
            UpdateModelStatus();
            return;
        }

        App.Settings.Current.SelectedModelFile = selectedModel;
        await App.Settings.SaveAsync();

        try
        {
            App.Transcription.Unload();
            if (App.ModelManager.IsModelDownloaded())
            {
                await App.Transcription.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load selected model '{selectedModel}': {ex.Message}",
                "Model Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        UpdateModelStatus();
    }

    private void OutputMode_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        
        if (PasteRadio.IsChecked == true)
        {
            App.Settings.Current.OutputMode = OutputMode.PasteToActiveWindow;
        }
        else
        {
            App.Settings.Current.OutputMode = OutputMode.CopyToClipboard;
        }
        _ = App.Settings.SaveAsync();
    }

    private void StartupCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        
        var enabled = StartupCheckbox.IsChecked == true;
        App.Settings.Current.RunOnStartup = enabled;
        App.Settings.SetRunOnStartup(enabled);
        _ = App.Settings.SaveAsync();
    }

    private void OverlayCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        
        App.Settings.Current.ShowOverlay = OverlayCheckbox.IsChecked == true;
        _ = App.Settings.SaveAsync();
    }

    private async void RedownloadButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will download the base speech model (ggml-base.en.bin). Continue?",
            "Download Model",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Unload current model
        App.Transcription.Unload();

        // Delete existing
        App.ModelManager.DeleteModel();

        // Show download progress
        var progressWindow = new DownloadProgressWindow();
        progressWindow.Owner = this;
        progressWindow.Show();

        try
        {
            await App.ModelManager.DownloadModelAsync();
            LoadModels();
            await App.Transcription.InitializeAsync();
            UpdateModelStatus();
            MessageBox.Show("Model downloaded and loaded successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to download model: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            progressWindow.Close();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isCapturingHotkey)
        {
            CancelHotkeyCapture();
        }
        base.OnClosing(e);
    }
}

/// <summary>
/// Helper class for microphone dropdown items
/// </summary>
public class MicrophoneItem
{
    public string Id { get; }
    public string Name { get; }

    public MicrophoneItem(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;
}

