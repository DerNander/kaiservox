using System.Runtime.InteropServices;
using System.Windows;
using KaiserVox.Helpers;
using KaiserVox.Models;

namespace KaiserVox.Services;

/// <summary>
/// Handles output of transcribed text (paste or clipboard)
/// </summary>
public class OutputService
{
    private readonly SettingsService _settings;
    private IntPtr _previousForegroundWindow;

    public OutputService(SettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Store the current foreground window before dictation starts
    /// </summary>
    public void SaveForegroundWindow()
    {
        _previousForegroundWindow = Win32.GetForegroundWindow();
    }

    /// <summary>
    /// Output the transcribed text according to settings
    /// </summary>
    public async Task OutputTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        switch (_settings.Current.OutputMode)
        {
            case OutputMode.PasteToActiveWindow:
                await PasteToWindowAsync(text);
                break;
            
            case OutputMode.CopyToClipboard:
                await CopyToClipboardAsync(text);
                break;
        }
    }

    /// <summary>
    /// Paste text to the previously active window using keyboard simulation
    /// </summary>
    private async Task PasteToWindowAsync(string text)
    {
        // Restore focus to the previous window
        if (_previousForegroundWindow != IntPtr.Zero)
        {
            Win32.ForceForegroundWindow(_previousForegroundWindow);
            await Task.Delay(50); // Small delay for window activation
        }

        // Use clipboard + Ctrl+V for reliability, then restore clipboard
        var previousClipboard = await GetClipboardTextAsync();
        
        await SetClipboardTextAsync(text);
        await Task.Delay(20);
        
        // Send Ctrl+V
        SendCtrlV();
        
        await Task.Delay(100);

        // Optionally restore previous clipboard content
        // (commented out as it can interfere with some apps)
        // if (previousClipboard != null)
        // {
        //     await SetClipboardTextAsync(previousClipboard);
        // }
    }

    /// <summary>
    /// Copy text to clipboard only
    /// </summary>
    private async Task CopyToClipboardAsync(string text)
    {
        await SetClipboardTextAsync(text);
    }

    /// <summary>
    /// Send Ctrl+V keystroke
    /// </summary>
    private void SendCtrlV()
    {
        var inputs = new Win32.INPUT[4];

        // Ctrl down
        inputs[0] = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            u = new Win32.InputUnion
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = 0x11, // VK_CONTROL
                    dwFlags = 0
                }
            }
        };

        // V down
        inputs[1] = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            u = new Win32.InputUnion
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = 0x56, // V key
                    dwFlags = 0
                }
            }
        };

        // V up
        inputs[2] = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            u = new Win32.InputUnion
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = 0x56,
                    dwFlags = Win32.KEYEVENTF_KEYUP
                }
            }
        };

        // Ctrl up
        inputs[3] = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            u = new Win32.InputUnion
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = 0x11,
                    dwFlags = Win32.KEYEVENTF_KEYUP
                }
            }
        };

        Win32.SendInput(4, inputs, Marshal.SizeOf(typeof(Win32.INPUT)));
    }

    /// <summary>
    /// Get current clipboard text
    /// </summary>
    private static async Task<string?> GetClipboardTextAsync()
    {
        string? result = null;
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    result = Clipboard.GetText();
                }
            }
            catch { }
        });
        return result;
    }

    /// <summary>
    /// Set clipboard text
    /// </summary>
    private static async Task SetClipboardTextAsync(string text)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch { }
        });
    }
}

