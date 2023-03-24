using Discord;
using Discord.Audio;
using NLog;
using System;
using System.Threading.Tasks;

namespace MagicConchBot.Helpers
{
    public static class AudioHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static async Task LeaveChannelAsync(IAudioClient audio)
        {
            if (audio != null && audio.ConnectionState == ConnectionState.Connected)
            {
                try
                {
                    await audio.StopAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }

        public static IVoiceChannel GetAudioChannel(IInteractionContext context)
        {
            return (context.User as IGuildUser)?.VoiceChannel;
        }

        public static async Task<IAudioClient> JoinChannelAsync(IAudioChannel channel)
        {
            try
            {
                if (channel != null)
                {
                    try
                    {
                        var client = await channel.ConnectAsync();
                        return client;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to join channel.");
            }

            return null;
        }
    }
}