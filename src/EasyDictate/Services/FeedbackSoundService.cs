using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;

namespace KaiserVox.Services;

/// <summary>
/// Plays short embedded UI feedback sounds for dictation state changes.
/// </summary>
public static class FeedbackSoundService
{
    private static readonly Uri ListeningUri = new("pack://application:,,,/Resources/Sounds/listening.wav", UriKind.Absolute);
    private static readonly Uri ProcessingUri = new("pack://application:,,,/Resources/Sounds/processing.wav", UriKind.Absolute);

    private static readonly Lazy<byte[]?> ListeningBytes = new(() => LoadResourceBytes(ListeningUri, "listening.wav"));
    private static readonly Lazy<byte[]?> ProcessingBytes = new(() => LoadResourceBytes(ProcessingUri, "processing.wav"));

    public static void ValidateResources()
    {
        _ = ListeningBytes.Value;
        _ = ProcessingBytes.Value;
    }

    public static void PlayListening()
    {
        Play(ListeningBytes.Value, "listening");
    }

    public static void PlayProcessing()
    {
        Play(ProcessingBytes.Value, "processing");
    }

    private static void Play(byte[]? soundBytes, string soundName)
    {
        try
        {
            if (!IsEnabled())
            {
                return;
            }

            if (soundBytes == null || soundBytes.Length == 0)
            {
                Debug.WriteLine($"FeedbackSoundService: '{soundName}' sound resource missing, using fallback system sound.");
                SystemSounds.Asterisk.Play();
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(soundBytes, writable: false);
                    using var player = new SoundPlayer(stream);
                    player.PlaySync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FeedbackSoundService: failed to play '{soundName}' sound: {ex.Message}");
                    SystemSounds.Asterisk.Play();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FeedbackSoundService: unexpected playback error: {ex.Message}");
        }
    }

    private static bool IsEnabled()
    {
        if (Application.Current == null)
        {
            return false;
        }

        try
        {
            return App.Settings?.Current?.PlaySounds ?? true;
        }
        catch
        {
            return true;
        }
    }

    private static byte[]? LoadResourceBytes(Uri resourceUri, string label)
    {
        try
        {
            if (Application.Current == null)
            {
                return null;
            }

            var info = Application.GetResourceStream(resourceUri);
            if (info?.Stream == null)
            {
                Debug.WriteLine($"FeedbackSoundService: embedded sound not found: {label}");
                return null;
            }

            using var input = info.Stream;
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FeedbackSoundService: failed loading '{label}' resource: {ex.Message}");
            return null;
        }
    }
}
