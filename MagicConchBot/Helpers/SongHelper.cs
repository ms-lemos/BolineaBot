using MagicConchBot.Common.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MagicConchBot.Helpers
{
    public static class SongHelper
    {
        public static readonly Regex UrlRegex =
            new(@"(\b(https?):\/\/)[-A-Za-z0-9+\/%?=_!.]+\.[-A-Za-z0-9+&#\/%=_]+");

        public static List<string> DisplaySongsClean(Song[] songs)
        {
            var output = new List<string>();
            var sb = new StringBuilder();

            for (var i = 0; i < songs.Length; i++)
            {
                if (sb.Length > 1500)
                {
                    output.Add(sb.ToString());
                    sb.Clear();
                }

                sb.Append($"`{(i + 1).ToString().PadLeft((int)Math.Log(songs.Length, 10) + 1)}.` : {songs[i].GetInfo()}");
            }

            return output;
        }
    }
}
