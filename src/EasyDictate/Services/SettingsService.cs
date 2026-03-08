using System.IO;
using System.Text.Json;
using System.Diagnostics;
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
    private const string StartupArgument = "--startup";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
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
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            var startupValueName = GetStartupValueName();

            if (enabled)
            {
                var exePath = ResolveExecutablePath();
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    Debug.WriteLine("Startup: failed to resolve executable path.");
                    return;
                }

                var command = BuildStartupCommand(exePath);
                key.SetValue(startupValueName, command, RegistryValueKind.String);
                key.DeleteValue(LegacyAppName, false);

                Debug.WriteLine($"Startup: writing registry value '{startupValueName}' => {command}");

                var writtenValue = key.GetValue(startupValueName)?.ToString();
                var verified = string.Equals(writtenValue, command, StringComparison.Ordinal);
                Debug.WriteLine(verified
                    ? $"Startup: verification OK for '{startupValueName}'."
                    : $"Startup: verification FAILED for '{startupValueName}'. Read back: {writtenValue ?? "<null>"}");
            }
            else
            {
                key.DeleteValue(startupValueName, false);
                key.DeleteValue(LegacyAppName, false);

                var stillExists = key.GetValue(startupValueName) is not null || key.GetValue(LegacyAppName) is not null;
                Debug.WriteLine(stillExists
                    ? $"Startup: removal verification FAILED. '{startupValueName}' or '{LegacyAppName}' still present."
                    : "Startup: removal verification OK.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if startup registry value currently points to this app executable
    /// </summary>
    public bool IsRunOnStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key == null) return false;

            var startupValueName = GetStartupValueName();
            var registryValue = key.GetValue(startupValueName)?.ToString()
                ?? key.GetValue(LegacyAppName)?.ToString();

            if (string.IsNullOrWhiteSpace(registryValue))
                return false;

            var configuredExe = ExtractExecutablePath(registryValue);
            var currentExe = ResolveExecutablePath();
            if (string.IsNullOrWhiteSpace(configuredExe) || string.IsNullOrWhiteSpace(currentExe))
                return false;

            var matchesPath = string.Equals(
                Path.GetFullPath(configuredExe),
                Path.GetFullPath(currentExe),
                StringComparison.OrdinalIgnoreCase);

            var hasStartupArgument = registryValue.Contains(StartupArgument, StringComparison.OrdinalIgnoreCase);
            return matchesPath && hasStartupArgument;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read startup state: {ex.Message}");
            return false;
        }
    }

    private static string GetStartupValueName()
    {
        if (!string.IsNullOrWhiteSpace(AppName))
            return AppName;

        var fallbackExe = ResolveExecutablePath();
        if (!string.IsNullOrWhiteSpace(fallbackExe))
        {
            var fileName = Path.GetFileNameWithoutExtension(fallbackExe);
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;
        }

        return "KaiserVox";
    }

    private static string BuildStartupCommand(string exePath)
    {
        return $"\"{exePath}\" {StartupArgument}";
    }

    private static string? ResolveExecutablePath()
    {
        string? mainModulePath = null;
        try
        {
            mainModulePath = Process.GetCurrentProcess().MainModule?.FileName;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Startup: failed to read Process.MainModule path: {ex.Message}");
        }

        var candidates = new[]
        {
            Environment.ProcessPath,
            mainModulePath,
            Environment.GetCommandLineArgs().FirstOrDefault()
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var normalized = candidate.Trim().Trim('"');
            if (!Path.IsPathRooted(normalized))
                continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(normalized);
            }
            catch
            {
                continue;
            }

            if (!File.Exists(fullPath))
                continue;

            if (!fullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;

            return fullPath;
        }

        return null;
    }

    private static string? ExtractExecutablePath(string startupCommand)
    {
        var value = startupCommand.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (value.StartsWith("\"", StringComparison.Ordinal))
        {
            var endQuote = value.IndexOf('"', 1);
            if (endQuote > 1)
                return value[1..endQuote];
            return null;
        }

        var firstSpace = value.IndexOf(' ');
        return firstSpace > 0 ? value[..firstSpace] : value;
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
