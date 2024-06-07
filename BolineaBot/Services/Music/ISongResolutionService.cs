using MagicConchBot.Common.Types;
using System;
using System.Threading.Tasks;

namespace MagicConchBot.Services
{
    public interface ISongResolutionService
    {
        Task<Song> ResolveSong(string url, TimeSpan startTime);
    }
}