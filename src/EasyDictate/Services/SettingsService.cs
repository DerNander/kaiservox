using System.IO;
using System.Text.Json;
using KaiserVox.Models;
using Microsoft.Win32;

namespace KaiserVox.Services;

/// <summary>
/// Manages application settings persistence
/// </summary>
public class SettingsService
{
    private const string AppName = "KaiserVox";
    private const string LegacyAppName = "EasyDictate";
    private const string ConfigFileName = "config.json";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Current application settings
    /// </summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>
    /// Application data folder path
    /// </summary>
    public string AppDataPath { get; }

    /// <summary>
    /// Models folder path
    /// </summary>
    public string ModelsPath { get; }

    /// <summary>
    /// Config file path
    /// </summary>
    public string ConfigPath { get; }

    public SettingsService()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        AppDataPath = Path.Combine(roaming, AppName);

        ModelsPath = Path.Combine(AppDataPath, "models");
        ConfigPath = Path.Combine(AppDataPath, ConfigFileName);

        MigrateLegacyDataIfNeeded(roaming);

        // Ensure directories exist
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(ModelsPath);
    }

    private void MigrateLegacyDataIfNeeded(string roamingPath)
    {
        try
        {
            var legacyPath = Path.Combine(roamingPath, LegacyAppName);

            if (!Directory.Exists(legacyPath) || Directory.Exists(AppDataPath))
                return;

            CopyDirectory(legacyPath, AppDataPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Legacy migration skipped: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, filePath);
            var targetPath = Path.Combine(destinationDir, relativePath);
            var targetParent = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetParent))
            {
                Directory.CreateDirectory(targetParent);
            }

            File.Copy(filePath, targetPath, overwrite: true);
        }
    }

    /// <summary>
    /// Load settings from disk
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            Current = new AppSettings();
        }
    }

    /// <summary>
    /// Save settings to disk
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Set or remove the app from Windows startup
    /// </summary>
    public void SetRunOnStartup(bool enabled)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
        }
    }

    /// <summary>
    /// Get display name for a hotkey configuration
    /// </summary>
    public static string GetHotkeyDisplayName(HotkeyModifiers modifiers, int key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            parts.Add("Win");

        // Convert virtual key code to name
        var keyName = key switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            >= 0x30 and <= 0x39 => ((char)key).ToString(), // 0-9
            >= 0x41 and <= 0x5A => ((char)key).ToString(), // A-Z
            >= 0x70 and <= 0x7B => $"F{key - 0x6F}",       // F1-F12
            0xC0 => "`",
            _ => $"Key{key:X2}"
        };

        parts.Add(keyName);
        return string.Join(" + ", parts);
    }
}

