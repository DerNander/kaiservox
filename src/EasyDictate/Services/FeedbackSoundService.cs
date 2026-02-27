using System.Windows;
using System.Windows.Media;

namespace KaiserVox.Services;

/// <summary>
/// Plays short embedded UI feedback sounds for dictation state changes.
/// </summary>
public static class FeedbackSoundService
{
    private static readonly object Sync = new();
    private static readonly List<MediaPlayer> ActivePlayers = new();

    private static readonly Uri ListeningUri = new("pack://application:,,,/Resources/Sounds/listening.wav", UriKind.Absolute);
    private static readonly Uri ProcessingUri = new("pack://application:,,,/Resources/Sounds/processing.wav", UriKind.Absolute);

    public static void PlayListening()
    {
        Play(ListeningUri, 0.28);
    }

    public static void PlayProcessing()
    {
        Play(ProcessingUri, 0.30);
    }

    private static void Play(Uri uri, double volume)
    {
        try
        {
            if (Application.Current == null || !App.Settings.Current.PlaySounds)
            {
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var player = new MediaPlayer
                    {
                        Volume = volume
                    };

                    player.MediaEnded += (_, _) => Cleanup(player);
                    player.MediaFailed += (_, _) => Cleanup(player);

                    lock (Sync)
                    {
                        ActivePlayers.Add(player);
                    }

                    player.Open(uri);
                    player.Play();
                }
                catch
                {
                    // Non-critical: sound feedback should never break dictation flow.
                }
            });
        }
        catch
        {
            // Ignore playback errors.
        }
    }

    private static void Cleanup(MediaPlayer player)
    {
        lock (Sync)
        {
            ActivePlayers.Remove(player);
        }

        try
        {
            player.Close();
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }
}
