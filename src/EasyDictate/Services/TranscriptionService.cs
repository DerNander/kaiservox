using System.IO;
using Whisper.net;

namespace KaiserVox.Services;

/// <summary>
/// Handles speech-to-text transcription using Whisper.net
/// </summary>
public class TranscriptionService : IDisposable
{
    private readonly ModelManager _modelManager;
    private readonly Models.AppSettings _settings;
    private WhisperProcessor? _processor;
    private bool _isInitialized;
    private bool _disposed;
    private readonly object _lock = new();

    /// <summary>
    /// Whether the transcription service is ready
    /// </summary>
    public bool IsReady => _isInitialized && _processor != null;

    /// <summary>
    /// Event raised when transcription completes
    /// </summary>
    public event EventHandler<TranscriptionResult>? TranscriptionCompleted;

    /// <summary>
    /// Event raised when transcription fails
    /// </summary>
    public event EventHandler<Exception>? TranscriptionFailed;

    public TranscriptionService(ModelManager modelManager, Models.AppSettings settings)
    {
        _modelManager = modelManager;
        _settings = settings;
    }

    /// <summary>
    /// Initialize the Whisper processor with the downloaded model
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        if (!_modelManager.IsModelDownloaded())
        {
            throw new InvalidOperationException("Model not downloaded. Please download the model first.");
        }

        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                var modelPath = _modelManager.ModelPath;
                var language = string.IsNullOrWhiteSpace(_settings.Language) ? "auto" : _settings.Language.Trim();

                var factory = WhisperFactory.FromPath(modelPath);

                _processor = factory.CreateBuilder()
                    .WithLanguage(language)
                    .Build();

                _isInitialized = true;
            }
        });
    }

    /// <summary>
    /// Transcribe audio data (WAV format, 16kHz mono)
    /// </summary>
    public async Task<string> TranscribeAsync(byte[] audioData)
    {
        if (!IsReady)
        {
            throw new InvalidOperationException("Transcription service not initialized.");
        }

        try
        {
            // Create a memory stream from the audio data
            using var audioStream = new MemoryStream(audioData);
            
            var segments = new List<string>();

            await Task.Run(async () =>
            {
                WhisperProcessor? processor;
                lock (_lock)
                {
                    processor = _processor;
                }
                
                if (processor == null) return;

                // Use the async enumerable API
                await foreach (var segment in processor.ProcessAsync(audioStream))
                {
                    var text = segment.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Filter out common Whisper hallucinations
                        if (!IsHallucination(text))
                        {
                            segments.Add(text);
                        }
                    }
                }
            });

            var result = string.Join(" ", segments).Trim();
            
            var transcriptionResult = new TranscriptionResult(result, audioData.Length);
            TranscriptionCompleted?.Invoke(this, transcriptionResult);
            
            return result;
        }
        catch (Exception ex)
        {
            TranscriptionFailed?.Invoke(this, ex);
            throw;
        }
    }

    /// <summary>
    /// Check if text is a common Whisper hallucination
    /// </summary>
    private static bool IsHallucination(string text)
    {
        // Common hallucinations from Whisper when there's silence or noise
        var hallucinations = new[]
        {
            "Thank you for watching",
            "Thanks for watching",
            "Subscribe",
            "Like and subscribe",
            "Please subscribe",
            "Thank you",
            "Bye",
            "Goodbye",
            "[Music]",
            "[Applause]",
            "(music)",
            "(applause)",
            "♪",
            "...",
            "you"
        };

        var lowerText = text.ToLowerInvariant().Trim();
        
        // Check exact matches or very short suspicious text
        if (lowerText.Length < 3) return true;
        
        foreach (var h in hallucinations)
        {
            if (lowerText.Equals(h.ToLowerInvariant()))
                return true;
        }

        // Check if it's just punctuation or music symbols
        if (text.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c) || c == '♪'))
            return true;

        return false;
    }

    /// <summary>
    /// Unload the model to free memory
    /// </summary>
    public void Unload()
    {
        lock (_lock)
        {
            _processor?.Dispose();
            _processor = null;
            _isInitialized = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Unload();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Result of a transcription
/// </summary>
public class TranscriptionResult
{
    public string Text { get; }
    public int AudioBytes { get; }
    public DateTime Timestamp { get; }

    public TranscriptionResult(string text, int audioBytes)
    {
        Text = text;
        AudioBytes = audioBytes;
        Timestamp = DateTime.Now;
    }
}
