using System.Text.Json.Serialization;

namespace KaiserVox.Models;

/// <summary>
/// Application settings persisted to config.json
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Whether this is the first run of the application
    /// </summary>
    public bool IsFirstRun { get; set; } = true;

    /// <summary>
    /// Modifier keys for the hotkey (Alt, Ctrl, Shift, Win)
    /// </summary>
    public HotkeyModifiers HotkeyModifiers { get; set; } = HotkeyModifiers.Alt;

    /// <summary>
    /// The key to press with modifiers
    /// </summary>
    public int HotkeyKey { get; set; } = 0x20; // VK_SPACE

    /// <summary>
    /// Selected microphone device ID (null = default)
    /// </summary>
    public string? MicrophoneDeviceId { get; set; }

    /// <summary>
    /// Output mode: paste to active window or copy to clipboard
    /// </summary>
    public OutputMode OutputMode { get; set; } = OutputMode.CopyToClipboard;

    /// <summary>
    /// Run on Windows startup
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Maximum recording duration in seconds
    /// </summary>
    public int MaxRecordingSeconds { get; set; } = 120;

    /// <summary>
    /// Show overlay window during dictation
    /// </summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>
    /// Play sound on recording start/stop
    /// </summary>
    public bool PlaySounds { get; set; } = true;

    /// <summary>
    /// Transcription language ("auto" for automatic detection)
    /// </summary>
    public string Language { get; set; } = "auto";

    /// <summary>
    /// Selected Whisper model filename in the models directory
    /// </summary>
    public string SelectedModelFile { get; set; } = "ggml-base.en.bin";
}

/// <summary>
/// Hotkey modifier flags matching Windows API
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

/// <summary>
/// How transcribed text is output
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// Simulate keystrokes to type into the active window
    /// </summary>
    PasteToActiveWindow,

    /// <summary>
    /// Copy text to clipboard only
    /// </summary>
    CopyToClipboard
}

