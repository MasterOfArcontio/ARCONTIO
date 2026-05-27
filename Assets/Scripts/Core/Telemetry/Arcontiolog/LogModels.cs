using System;
using System.Collections.Generic;

namespace Arcontio.Core.Logging
{
    public enum LogLevel { Trace = 0, Debug = 1, Info = 2, Warn = 3, Error = 4, Fatal = 5 }

    public readonly struct LogContext
    {
        public readonly long Tick;
        public readonly string Channel;   // es: "Sim", "World", "Memory", "Tokens", "UI"
        public readonly int NpcId;        // opzionale (0 se non applicabile)
        public readonly (int x, int y)? Cell; // opzionale
        public LogContext(long tick, string channel, int npcId = 0, (int, int)? cell = null)
        {
            Tick = tick;
            Channel = channel ?? "Core";
            NpcId = npcId;
            Cell = cell;
        }
    }

    public sealed class LogBlock
    {
        public LogLevel Level;
        public string MessageKey;                 // chiave localizzata (es: "log.event.emitted")
        public string MessageFallback;            // fallback raw se vuoi bypassare loc
        public readonly List<(string key, string value)> Fields = new();
        public readonly List<string> Lines = new();
        public Exception Exception;

        public LogBlock(LogLevel level, string messageKey = null, string messageFallback = null)
        {
            Level = level;
            MessageKey = messageKey;
            MessageFallback = messageFallback;
        }

        public LogBlock AddField(string key, object value)
        {
            Fields.Add((key ?? "", value?.ToString() ?? "null"));
            return this;
        }

        public LogBlock AddLine(string line)
        {
            Lines.Add(line ?? "");
            return this;
        }

        public LogBlock WithException(Exception ex)
        {
            Exception = ex;
            return this;
        }
    }

}
