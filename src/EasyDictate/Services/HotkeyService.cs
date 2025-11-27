using System.Windows;
using System.Windows.Interop;
using EasyDictate.Helpers;
using EasyDictate.Models;

namespace EasyDictate.Services;

/// <summary>
/// Manages global hotkey registration for push-to-talk
/// </summary>
public class HotkeyService : IDisposable
{
    private const int HOTKEY_ID = 9000;
    
    private IntPtr _windowHandle;
    private HwndSource? _source;
    private Action? _onKeyDown;
    private Action? _onKeyUp;
    private int _registeredKey;
    private bool _isKeyHeld;
    private System.Windows.Threading.DispatcherTimer? _keyPollTimer;
    private bool _disposed;

    /// <summary>
    /// Whether the hotkey is currently registered
    /// </summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// Whether dictation is currently active (key held down)
    /// </summary>
    public bool IsActive => _isKeyHeld;

    /// <summary>
    /// Register a push-to-talk hotkey
    /// </summary>
    public void RegisterPushToTalk(HotkeyModifiers modifiers, int key, Action onKeyDown, Action onKeyUp)
    {
        _onKeyDown = onKeyDown;
        _onKeyUp = onKeyUp;
        _registeredKey = key;

        // Create a hidden window for receiving hotkey messages
        var helper = new WindowInteropHelper(new Window { ShowInTaskbar = false, WindowStyle = WindowStyle.None });
        helper.EnsureHandle();
        _windowHandle = helper.Handle;
        
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);

        // Register the hotkey
        uint mod = (uint)modifiers;
        if (!Win32.RegisterHotKey(_windowHandle, HOTKEY_ID, mod, (uint)key))
        {
            throw new InvalidOperationException($"Failed to register hotkey. It may already be in use by another application.");
        }

        IsRegistered = true;

        // Start polling for key release (since we only get hotkey press notification)
        _keyPollTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _keyPollTimer.Tick += PollKeyState;
    }

    /// <summary>
    /// Unregister the current hotkey
    /// </summary>
    public void Unregister()
    {
        if (!IsRegistered) return;

        _keyPollTimer?.Stop();
        Win32.UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _source?.RemoveHook(WndProc);
        _source?.Dispose();
        _source = null;
        IsRegistered = false;
        _isKeyHeld = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            if (!_isKeyHeld)
            {
                _isKeyHeld = true;
                _keyPollTimer?.Start();
                
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    _onKeyDown?.Invoke();
                });
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void PollKeyState(object? sender, EventArgs e)
    {
        // Check if the main key is still held
        if (!Win32.IsKeyDown(_registeredKey))
        {
            _isKeyHeld = false;
            _keyPollTimer?.Stop();
            
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                _onKeyUp?.Invoke();
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        Unregister();
        GC.SuppressFinalize(this);
    }
}

