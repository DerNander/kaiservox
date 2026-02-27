using System.Windows;

namespace KaiserVox.Services;

/// <summary>
/// Coordinates the dictation workflow: record -> transcribe -> output
/// </summary>
public class DictationCoordinator
{
    private readonly AudioCaptureService _audioCapture;
    private readonly TranscriptionService _transcription;
    private readonly OutputService _output;
    
    private Views.OverlayWindow? _overlayWindow;
    private bool _isProcessing;

    /// <summary>
    /// Current state of the dictation process
    /// </summary>
    public DictationState State { get; private set; } = DictationState.Idle;

    /// <summary>
    /// Event raised when state changes
    /// </summary>
    public event EventHandler<DictationState>? StateChanged;

    /// <summary>
    /// Event raised when transcription is complete
    /// </summary>
    public event EventHandler<string>? TranscriptionComplete;

    /// <summary>
    /// Event raised on error
    /// </summary>
    public event EventHandler<string>? Error;

    public DictationCoordinator(
        AudioCaptureService audioCapture,
        TranscriptionService transcription,
        OutputService output)
    {
        _audioCapture = audioCapture;
        _transcription = transcription;
        _output = output;

        // Wire up events
        _audioCapture.RecordingStarted += OnRecordingStarted;
        _audioCapture.RecordingStopped += OnRecordingStopped;
    }

    /// <summary>
    /// Start dictation (called on hotkey down)
    /// </summary>
    public void StartDictation()
    {
        if (_isProcessing || State != DictationState.Idle)
        {
            return;
        }

        if (!_transcription.IsReady)
        {
            ShowError("Transcription model not loaded. Please check Settings.");
            return;
        }

        try
        {
            // Save the current foreground window before we potentially steal focus
            _output.SaveForegroundWindow();
            
            // Show overlay
            ShowOverlay();
            
            // Start recording
            _audioCapture.StartRecording();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to start recording: {ex.Message}");
            HideOverlay();
        }
    }

    /// <summary>
    /// Stop dictation (called on hotkey up)
    /// </summary>
    public void StopDictation()
    {
        if (State != DictationState.Listening)
        {
            return;
        }

        _audioCapture.StopRecording();
    }

    private void OnRecordingStarted(object? sender, EventArgs e)
    {
        SetState(DictationState.Listening);
        FeedbackSoundService.PlayListening();
    }

    private async void OnRecordingStopped(object? sender, byte[] audioData)
    {
        try
        {
            if (audioData.Length == 0)
            {
                SetState(DictationState.Idle);
                HideOverlay();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Audio data received: {audioData.Length} bytes");

            _isProcessing = true;
            SetState(DictationState.Transcribing);
            FeedbackSoundService.PlayProcessing();

            System.Diagnostics.Debug.WriteLine("Starting transcription...");
            var text = await _transcription.TranscribeAsync(audioData);
            System.Diagnostics.Debug.WriteLine($"Transcription result: '{text}'");

            if (!string.IsNullOrWhiteSpace(text))
            {
                System.Diagnostics.Debug.WriteLine("Outputting text...");
                await _output.OutputTextAsync(text);
                TranscriptionComplete?.Invoke(this, text);
                
                // Show success message based on output mode
                if (App.Settings.Current.OutputMode == Models.OutputMode.CopyToClipboard)
                {
                    ShowSuccess("Copied!\nPress Ctrl+V to paste");
                }
                else
                {
                    ShowSuccess("Copied and Pasted!");
                }
            }
            else
            {
                ShowSuccess("No speech detected");
            }

            SetState(DictationState.Idle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in OnRecordingStopped: {ex}");
            SetState(DictationState.Error);
            
            // Show error in message box since overlay might not work
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show($"Transcription failed:\n\n{ex.Message}\n\nDetails: {ex.GetType().Name}",
                    "KaiserVox Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            HideOverlay();
        }
        finally
        {
            _isProcessing = false;
            // Don't hide overlay here - ShowSuccess/ShowError handle their own auto-hide
        }
    }

    private void SetState(DictationState state)
    {
        State = state;
        StateChanged?.Invoke(this, state);
        
        // Update overlay if visible
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _overlayWindow?.UpdateState(state);
        });
    }

    private void ShowOverlay()
    {
        if (!App.Settings.Current.ShowOverlay) return;

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _overlayWindow ??= new Views.OverlayWindow();
            _overlayWindow.Show();
            _overlayWindow.UpdateState(State);
        });
    }

    private void HideOverlay()
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _overlayWindow?.Hide();
        });
    }

    private void ShowError(string message)
    {
        Error?.Invoke(this, message);
        
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // Show as a non-blocking notification via the overlay
            _overlayWindow ??= new Views.OverlayWindow();
            _overlayWindow.ShowError(message);
        });
    }

    private void ShowSuccess(string message)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _overlayWindow ??= new Views.OverlayWindow();
            _overlayWindow.ShowSuccess(message);
        });
    }
}

/// <summary>
/// States of the dictation process
/// </summary>
public enum DictationState
{
    Idle,
    Listening,
    Transcribing,
    Error
}

