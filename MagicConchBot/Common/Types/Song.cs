using System;
using System.Threading;
using Discord;
using MagicConchBot.Resources;

namespace MagicConchBot.Common.Types
{
    public class Song
    {
        public bool IsPaused { get; set; }

        public string Name { get; }
        public string Url { get; }
        public string DirectUrl { get; set; }
        private string ThumbnailUrl { get; }

        public TimeSpan SeekTo { get; set; }

        private TimeSpan Length { get; } // Length in seconds
        public TimeSpan CurrentTime { get; set; }

        private string TotalTimePretty => Length > new TimeSpan(0, 59, 59) ? Length.ToString(@"hh\:mm\:ss") : (Length == TimeSpan.Zero ? "??" : Length.ToString(@"mm\:ss"));
        private string CurrentTimePretty => Length > new TimeSpan(0, 59, 59) ? CurrentTime.ToString(@"hh\:mm\:ss") : CurrentTime.ToString(@"mm\:ss");

        public CancellationTokenSource TokenSource;

        public Song(string name, TimeSpan length, string url, string thumbnailUrl = "")
            : this(name, length, url, thumbnailUrl, TimeSpan.Zero)
        {
        }

        public Song(string name, TimeSpan length, string url, string thumbnailUrl, TimeSpan seekTo)
        {
            ThumbnailUrl = thumbnailUrl;
            Name = name;
            Length = length;
            Url = url;
            SeekTo = seekTo;
        }

        public Embed GetEmbed(string title = "", bool embedThumbnail = true)
        {
            var embed = new EmbedBuilder { Color = Constants.MaterialBlue };
            if (title == "")
            {
                embed.AddField(x =>
                {
                    x.WithName($"[Current] {Name}")
                        .WithValue($"**Url**:\n{Url}\n\n**Duration**:\n{CurrentTimePretty} / {TotalTimePretty}");
                });
                if (ThumbnailUrl != "" && embedThumbnail)
                    embed.WithThumbnailUrl(ThumbnailUrl);
            }
            else
            {
                embed.AddField(x =>
                {
                    x.WithName(title)
                        .WithValue($"**Url**:\n{Url}\n\n**Duration**:\n{TotalTimePretty}");
                });
                if (ThumbnailUrl != "" && embedThumbnail)
                    embed.WithThumbnailUrl(ThumbnailUrl);
            }
            return embed.Build();
        }
    }
}