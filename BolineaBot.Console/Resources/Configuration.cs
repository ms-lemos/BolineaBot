using System;

namespace MagicConchBot.Resources
{
    public static class Configuration
    {
        public static string Token { get; set; } =
            Environment.GetEnvironmentVariable(Constants.DiscordTokenVariable);

        public static string SoundCloudClientSecret { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SoundCloudClientSecretVariable);
        
        public static string SoundCloudClientId { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SoundCloudClientIdVariable);

        public static string SpotifyClientId { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SpotifyClientId);

        public static string SpotifyClientSecret { get; set; } =
            Environment.GetEnvironmentVariable(Constants.SpotifyClientSecret);
    }
}