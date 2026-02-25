using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace KaiserVox.Services;

/// <summary>
/// Handles microphone audio capture for transcription
/// </summary>
public class AudioCaptureService : IDisposable
{
    private readonly SettingsService _settings;
    private WasapiCapture? _capture;
    private MemoryStream? _audioBuffer;
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;
    private bool _disposed;

    // Whisper expects 16kHz mono audio
    private const int TargetSampleRate = 16000;
    private const int TargetChannels = 1;

    /// <summary>
    /// Whether recording is currently active
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Event raised when recording starts
    /// </summary>
    public event EventHandler? RecordingStarted;

    /// <summary>
    /// Event raised when recording stops
    /// </summary>
    public event EventHandler<byte[]>? RecordingStopped;

    public AudioCaptureService(SettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Get list of available microphone devices
    /// </summary>
    public static List<(string Id, string Name)> GetMicrophoneDevices()
    {
        var devices = new List<(string, string)>();
        
        var enumerator = new MMDeviceEnumerator();
        
        // Add default device
        try
        {
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            devices.Add(("default", $"Default - {defaultDevice.FriendlyName}"));
        }
        catch { }

        // Add all capture devices
        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            devices.Add((device.ID, device.FriendlyName));
        }

        return devices;
    }

    /// <summary>
    /// Start recording from the microphone
    /// </summary>
    public void StartRecording()
    {
        if (_isRecording) return;

        try
        {
            var enumerator = new MMDeviceEnumerator();
            MMDevice device;

            // Get the selected device or default
            if (string.IsNullOrEmpty(_settings.Current.MicrophoneDeviceId) || 
                _settings.Current.MicrophoneDeviceId == "default")
            {
                device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            else
            {
                device = enumerator.GetDevice(_settings.Current.MicrophoneDeviceId);
            }

            // Create capture
            _capture = new WasapiCapture(device);
            
            // Setup buffer for audio data
            _audioBuffer = new MemoryStream();
            
            // Create a wave format for output (we'll resample if needed)
            var targetFormat = new WaveFormat(TargetSampleRate, 16, TargetChannels);
            _waveWriter = new WaveFileWriter(_audioBuffer, targetFormat);

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;

            _capture.StartRecording();
            _isRecording = true;
            
            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start recording: {ex.Message}");
            CleanupRecording();
            throw;
        }
    }

    /// <summary>
    /// Stop recording and return the audio data
    /// </summary>
    public void StopRecording()
    {
        if (!_isRecording) return;

        _capture?.StopRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveWriter == null || _capture == null) return;

        // Convert to target format if needed
        var sourceFormat = _capture.WaveFormat;
        
        if (sourceFormat.SampleRate != TargetSampleRate || sourceFormat.Channels != TargetChannels)
        {
            // Resample the audio
            var resampled = ResampleAudio(e.Buffer, e.BytesRecorded, sourceFormat);
            _waveWriter.Write(resampled, 0, resampled.Length);
        }
        else
        {
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private byte[] ResampleAudio(byte[] buffer, int bytesRecorded, WaveFormat sourceFormat)
    {
        // Simple resampling - convert to float samples, resample, convert back
        using var sourceStream = new RawSourceWaveStream(new MemoryStream(buffer, 0, bytesRecorded), sourceFormat);
        using var resampler = new MediaFoundationResampler(sourceStream, new WaveFormat(TargetSampleRate, 16, TargetChannels));
        resampler.ResamplerQuality = 60;

        using var outputStream = new MemoryStream();
        var resampledBuffer = new byte[4096];
        int read;
        while ((read = resampler.Read(resampledBuffer, 0, resampledBuffer.Length)) > 0)
        {
            outputStream.Write(resampledBuffer, 0, read);
        }
        return outputStream.ToArray();
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;

        byte[] audioData = Array.Empty<byte>();

        if (_waveWriter != null && _audioBuffer != null)
        {
            _waveWriter.Flush();
            audioData = _audioBuffer.ToArray();
        }

        CleanupRecording();
        
        RecordingStopped?.Invoke(this, audioData);
    }

    private void CleanupRecording()
    {
        _waveWriter?.Dispose();
        _waveWriter = null;
        
        _audioBuffer?.Dispose();
        _audioBuffer = null;

        if (_capture != null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
            _capture = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CleanupRecording();
        GC.SuppressFinalize(this);
    }
}

