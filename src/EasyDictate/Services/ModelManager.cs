using System.IO;
using System.Net.Http;

namespace EasyDictate.Services;

/// <summary>
/// Manages Whisper model download and storage
/// </summary>
public class ModelManager
{
    private readonly SettingsService _settings;
    private readonly HttpClient _httpClient;

    // Using ggml-base.en for fast English transcription (~140MB, very fast)
    private const string ModelFileName = "ggml-base.en.bin";
    private const string ModelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin";
    private const long ExpectedModelSize = 140_000_000; // ~140MB
    
    public string ModelPath => Path.Combine(_settings.ModelsPath, ModelFileName);

    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    public ModelManager(SettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromHours(2)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EasyDictate/1.0");
    }

    public bool IsModelDownloaded()
    {
        if (!File.Exists(ModelPath)) return false;
        var fileInfo = new FileInfo(ModelPath);
        return fileInfo.Length > ExpectedModelSize * 0.9;
    }

    public async Task DownloadModelAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Ensure directory exists
        try
        {
            Directory.CreateDirectory(_settings.ModelsPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create models directory '{_settings.ModelsPath}': {ex.Message}", ex);
        }

        // Step 2: Create temp file path in Windows temp folder
        var tempPath = Path.Combine(Path.GetTempPath(), $"easydictate_{Guid.NewGuid():N}.bin");

        // Step 3: Make HTTP request
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"HTTP request failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Server returned error: {response.StatusCode} {response.ReasonPhrase}");
        }

        var totalBytes = response.Content.Headers.ContentLength ?? ExpectedModelSize;

        // Step 4: Get content stream
        Stream contentStream;
        try
        {
            contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read HTTP response stream: {ex.Message}", ex);
        }

        // Step 5: Create temp file
        FileStream fileStream;
        try
        {
            fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create temp file '{tempPath}': {ex.Message}", ex);
        }

        try
        {
            // Step 6: Download loop
            var buffer = new byte[81920];
            var downloadedBytes = 0L;
            int bytesRead;
            var lastProgressReport = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                downloadedBytes += bytesRead;

                if (DateTime.UtcNow - lastProgressReport > TimeSpan.FromMilliseconds(200))
                {
                    var progress = (double)downloadedBytes / totalBytes * 100;
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(downloadedBytes, totalBytes, progress));
                    lastProgressReport = DateTime.UtcNow;
                }
            }

            DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(totalBytes, totalBytes, 100));

            // Step 7: Close file stream
            await fileStream.FlushAsync(cancellationToken);
            fileStream.Close();
            await fileStream.DisposeAsync();
        }
        catch (Exception ex)
        {
            fileStream.Close();
            await fileStream.DisposeAsync();
            try { File.Delete(tempPath); } catch { }
            throw new Exception($"Failed during download: {ex.Message}", ex);
        }

        // Step 8: Move to final location
        try
        {
            if (File.Exists(ModelPath))
            {
                File.Delete(ModelPath);
            }
            File.Move(tempPath, ModelPath);
        }
        catch (Exception ex)
        {
            try { File.Delete(tempPath); } catch { }
            throw new Exception($"Failed to move file to '{ModelPath}': {ex.Message}", ex);
        }
    }

    public void DeleteModel()
    {
        if (File.Exists(ModelPath))
        {
            File.Delete(ModelPath);
        }
    }

    public string GetModelSizeDisplay()
    {
        if (!File.Exists(ModelPath)) return "Not downloaded";
        var size = new FileInfo(ModelPath).Length;
        return FormatBytes(size);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

public class DownloadProgressEventArgs : EventArgs
{
    public long BytesDownloaded { get; }
    public long TotalBytes { get; }
    public double PercentComplete { get; }

    public DownloadProgressEventArgs(long bytesDownloaded, long totalBytes, double percentComplete)
    {
        BytesDownloaded = bytesDownloaded;
        TotalBytes = totalBytes;
        PercentComplete = percentComplete;
    }
}
