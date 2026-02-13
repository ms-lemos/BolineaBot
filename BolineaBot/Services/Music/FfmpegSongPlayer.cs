using Discord;
using Discord.Audio;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MagicConchBot.Services.Music
{
    public class FfmpegSongPlayer : ISongPlayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const float MaxVolume = 1f;
        private const float MinVolume = 0f;
        private const int Milliseconds = 20;
        private int FrameSize { get; set; } = 3840;
        private const int MaxRetryCount = 50;
        private Song currentSong;
        private IAudioClient audioClient;
        private IMessageChannel messageChannel;
        private CancellationTokenSource tokenSource;
        private Task _currentPlaying;

        private int Bitrate { get; set; }
        private float Volume { get; set; } = 1f;

        public event AsyncEventHandler<SongArgs> OnSongCompleted;
        public event AsyncEventHandler<SongErrorArgs> OnSongError;

        private Task HandleSongCompleted() => OnSongCompleted?.Invoke(this, new(audioClient, messageChannel, currentSong, Bitrate));
        private Task HandleSongError(Exception ex) => OnSongError?.Invoke(this, new(ex, audioClient, messageChannel, currentSong, Bitrate));

        public async void PlaySong(IAudioClient client, IMessageChannel channel, Song song, int bitrate)
        {
            Bitrate = bitrate;
            currentSong = song;
            messageChannel = channel;
            audioClient = client;
            tokenSource = new CancellationTokenSource();

            try
            {
                await PlaySong(channel);
                await HandleSongCompleted();
            }
            catch (OperationCanceledException ex)
            {
                Log.Debug($"Player task cancelled: {ex.Message}");
            }
            catch (Exception ex)
            {
                await HandleSongError(ex);
            }
        }

        public Task Stop()
        {
            currentSong.Time.StartTime = TimeSpan.Zero;

            tokenSource.Cancel();

            return Task.CompletedTask;
        }

        public Task Pause()
        {
            currentSong.Time.StartTime = currentSong.Time.CurrentTime;

            tokenSource.Cancel();

            return Task.CompletedTask;
        }

        public void SetVolume(float volume)
        {
            Volume = Math.Clamp(volume, MinVolume, MaxVolume);
        }

        public float GetVolume()
        {
            return Volume;
        }

        public bool IsPlaying()
        {
            return !_currentPlaying?.IsCompleted ?? false;
        }

        private async Task PlaySong(IMessageChannel channel)
        {
            if (audioClient == null || audioClient.ConnectionState != ConnectionState.Connected)
            {
                Log.Warn("Audio client not connected; skipping playback.");
                return;
            }

            using var process = StartFfmpeg(currentSong);
            using var inStream = process.StandardOutput.BaseStream;


            tokenSource.Token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.CloseMainWindow();
                    }
                }
                catch (Exception ex) { Log.Error(ex); }
            });

            if (!currentSong.IsResolved)
            {
                var message = $"Skippped unresolved song: {currentSong.Name}";
                await channel.SendMessageAsync(message);
                Log.Info(message);
                return;
            }

            FrameSize = 4096;
            using AudioOutStream outStream = audioClient.CreatePCMStream(AudioApplication.Music, Bitrate);
            _currentPlaying = StreamAudio(currentSong, inStream, outStream);
            await _currentPlaying;
        }

        private async Task StreamAudio(Song song, Stream inStream, AudioOutStream outStream)
        {
            var buffer = new byte[FrameSize];
            var retryCount = 0;

            Log.Debug("Playing song.");
            song.Time.CurrentTime = song.Time.StartTime.GetValueOrDefault(TimeSpan.Zero);

            while (!tokenSource.IsCancellationRequested)
            {
                var byteCount = await inStream.ReadAsync(buffer.AsMemory(0, buffer.Length), tokenSource.Token);

                if (byteCount == 0)
                {
                    if (song.Time.Length != TimeSpan.Zero && song.Time.Length - song.Time.CurrentTime.GetValueOrDefault() <= TimeSpan.FromMilliseconds(1000))
                    {
                        Log.Debug("Read 0 bytes but song is finished.");
                        break;
                    }

                    await Task.Delay(100, tokenSource.Token).ConfigureAwait(false);

                    if (++retryCount == MaxRetryCount)
                    {
                        Log.Warn($"Failed to read from ffmpeg. Retries: {retryCount}");
                        break;
                    }
                }
                else
                {
                    retryCount = 0;
                }


                if (outStream.CanWrite)
                {
                    await outStream.WriteAsync(buffer.AsMemory(0, byteCount), tokenSource.Token);
                }

                song.Time.CurrentTime += CalculateCurrentTime(byteCount);

            }

            await outStream.FlushAsync(tokenSource.Token);
        }

        private static Process StartFfmpeg(Song song)
        {
            var seek = song.Time.StartTime.HasValue ? $"-ss {song.Time.StartTime.Value}" : string.Empty;

            //var arguments = $"-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36 Edg/112.0.1722.68\" -referer \"{song.OriginalUrl}\" -hide_banner -loglevel warning -re -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i \"{song.DefaultStreamUri}\" {seek} -ac 2 -f s16le -vn -ar 48000 pipe:";
            var arguments = $"-hide_banner -loglevel warning -re -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -err_detect ignore_err -i \"{song.DefaultStreamUri}\" {seek} -ac 2 -f s16le -vn -ar 48000 pipe:";

            Log.Debug(arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var process = new Process()
            {
                StartInfo = startInfo
            };

            process.ErrorDataReceived += (sender, data) =>
            {
                if (!string.IsNullOrWhiteSpace(data?.Data)) { Log.Error("Ffmpeg error: " + data.Data); }
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            if (process == null)
            {
                throw new Exception("Could not start FFMPEG");
            }

            if (process.StandardOutput.BaseStream == null)
            {
                throw new Exception("FFMPEG stream was not created.");
            }

            return process;
        }

        private TimeSpan CalculateCurrentTime(int currentBytes)
        {
            return TimeSpan.FromSeconds(currentBytes /
                                        (1000d * FrameSize /
                                         Milliseconds));
        }
    }
}
