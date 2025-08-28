using Microsoft.Extensions.Logging;

namespace TwitchSharp
{
    public class TwitchSharpLogs
    {
        string Prefix = "[{DateTime} TwitchSharp - {LogLevel}] ";
        string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
        ConsoleColor PrefixColor = ConsoleColor.Magenta;
        LogLevel MinimalLogLevel = LogLevel.Information;

        public event Action<string, LogLevel, Exception?>? OnLog; // CS8618

        public void ConfigureLogging(LogConfig conf)
        {
            if (conf.Prefix != null) Prefix = conf.Prefix;
            if (conf.PrefixColor != null) PrefixColor = (ConsoleColor)conf.PrefixColor;
            if (conf.MinimalLogLevel != null) MinimalLogLevel = (LogLevel)conf.MinimalLogLevel;
        }
        public void Log(string content, LogLevel logLevel, Exception? ex = null)
        {
            OnLog?.Invoke(content, logLevel, ex);
            if (logLevel < MinimalLogLevel) return;
            
            WritePrefix(logLevel);
            Console.WriteLine(content);

            if (ex != null)
            {
                WritePrefix(logLevel);
                Console.WriteLine("Thrown Exception: ");
                Console.WriteLine(ex.Message);
            }
        }
        private void WritePrefix(LogLevel logLevel)
        {
            string prefix = Prefix.Replace("{DateTime}", DateTime.Now.ToString(DateTimeFormat));
            string[] prefixArray =
            [
                prefix.Substring(0, prefix.IndexOf("{LogLevel}")),

                prefix.Substring(prefix.IndexOf("{LogLevel}"), 10)
                    .Replace("{LogLevel}", logLevel.ToString())
                    .PadRight(12, ' '),

                prefix.Substring(prefix.IndexOf("{LogLevel}") + 10)
            ];

            Console.ForegroundColor = PrefixColor;
            Console.Write(prefixArray[0]);
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Information => ConsoleColor.Blue,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White,
            };
            Console.Write(prefixArray[1]);
            Console.ForegroundColor = PrefixColor;
            Console.Write(prefixArray[2]);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
    public class LogConfig
    {
        public string? Prefix { get; set; } = null;
        public string? DateTimeFormat { get; set; } = null;
        public ConsoleColor? PrefixColor { get; set; } = null;
        public LogLevel? MinimalLogLevel { get; set; } = null;
    }
}