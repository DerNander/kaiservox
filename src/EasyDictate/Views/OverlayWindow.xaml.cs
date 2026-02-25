using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KaiserVox.Services;

namespace KaiserVox.Views;

/// <summary>
/// Overlay window showing dictation state
/// </summary>
public partial class OverlayWindow : Window
{
    private System.Windows.Threading.DispatcherTimer? _hideTimer;
    private DoubleAnimation? _pulseAnimation;
    private bool _showingMessage; // Flag to prevent UpdateState from hiding during success/error message

    public OverlayWindow()
    {
        InitializeComponent();
        PositionWindow();
    }

    private void PositionWindow()
    {
        // Position in bottom-right corner of primary screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    /// <summary>
    /// Update the overlay to reflect current state
    /// </summary>
    public void UpdateState(DictationState state)
    {
        // Don't change overlay if showing a success/error message
        if (_showingMessage) return;
        
        _hideTimer?.Stop();

        switch (state)
        {
            case DictationState.Listening:
                StatusText.Text = "Listening...";
                StateIndicator.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                StartPulseAnimation();
                Show();
                break;

            case DictationState.Transcribing:
                StatusText.Text = "Transcribing...";
                StateIndicator.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
                StartPulseAnimation();
                break;

            case DictationState.Error:
                StatusText.Text = "Error";
                StateIndicator.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                StopPulseAnimation();
                break;

            case DictationState.Idle:
            default:
                StopPulseAnimation();
                Hide();
                break;
        }
    }

    /// <summary>
    /// Show an error message briefly
    /// </summary>
    public void ShowError(string message)
    {
        _showingMessage = true;
        StatusText.Text = message.Length > 30 ? message[..27] + "..." : message;
        StateIndicator.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
        StopPulseAnimation();
        
        Show();
        AutoHideAfter(3);
    }

    /// <summary>
    /// Show a success message briefly
    /// </summary>
    public void ShowSuccess(string message)
    {
        _showingMessage = true;
        StatusText.Text = message;
        StateIndicator.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
        StopPulseAnimation();
        
        Show();
        AutoHideAfter(2);
    }

    private void AutoHideAfter(int seconds)
    {
        _hideTimer?.Stop();
        _hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(seconds)
        };
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            _showingMessage = false; // Reset flag when auto-hiding
            Hide();
        };
        _hideTimer.Start();
    }

    private void StartPulseAnimation()
    {
        try
        {
            _pulseAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0.4,
                Duration = TimeSpan.FromSeconds(0.6),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            StateIndicator.BeginAnimation(OpacityProperty, _pulseAnimation);
        }
        catch
        {
            StateIndicator.Opacity = 1;
        }
    }

    private void StopPulseAnimation()
    {
        try
        {
            StateIndicator.BeginAnimation(OpacityProperty, null);
            StateIndicator.Opacity = 1;
        }
        catch
        {
            // Ignore animation errors
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _hideTimer?.Stop();
        StopPulseAnimation();
        base.OnClosed(e);
    }
}
