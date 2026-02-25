using System.Windows;
using KaiserVox.Services;

namespace KaiserVox.Views;

/// <summary>
/// Simple progress window for model download
/// </summary>
public partial class DownloadProgressWindow : Window
{
    public DownloadProgressWindow()
    {
        InitializeComponent();
        
        // Subscribe to download progress
        App.ModelManager.DownloadProgress += OnProgress;
    }

    private void OnProgress(object? sender, DownloadProgressEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = e.PercentComplete;
            
            var mbDownloaded = e.BytesDownloaded / 1024.0 / 1024.0;
            var mbTotal = e.TotalBytes / 1024.0 / 1024.0;
            StatusText.Text = $"{mbDownloaded:F1} / {mbTotal:F1} MB ({e.PercentComplete:F0}%)";
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        App.ModelManager.DownloadProgress -= OnProgress;
        base.OnClosed(e);
    }
}

