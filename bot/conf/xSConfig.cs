using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using xSophBot.bot.logs;

namespace xSophBot.bot.conf
{
    public class xSConfig
    {
        public static LogLevel LogLevel;
        public static class Twitch
        {
#pragma warning disable CS8618
            public static string ClientId;
            public static string ClientSecret;
            public static string RefreshToken;
            public static string UserName;
        }
        public static class AI
        {
#pragma warning disable CS8618
            public static string GeminiKey;
            public static string SystemInstructions;
        }

        public static async ValueTask ReadConfigAsync()
        {
            try
            {
                StreamReader sr = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}/config/twitch.conf");
                xSLogger.Log(LogLevel.Debug, $"Reading file: twitch.conf", "xSConfig.cs");
                string confFile = await sr.ReadToEndAsync();
                xSLogger.Log(LogLevel.Debug, confFile, "twitch.conf");

                Twitch.ClientId = getValue(confFile, "ID");
                Twitch.ClientSecret = getValue(confFile, "Secret");
                Twitch.RefreshToken = getValue(confFile, "Refresh");
                Twitch.UserName = getValue(confFile, "Username");


                sr = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}/config/ai.conf");
                xSLogger.Log(LogLevel.Debug, $"Reading file: ai.conf", "xSConfig.cs");
                confFile = await sr.ReadToEndAsync();
                xSLogger.Log(LogLevel.Debug, confFile, "ai.conf");

                AI.GeminiKey = getValue(confFile, "Gemini_Key");
                sr = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}/config/ai.promt");
                xSLogger.Log(LogLevel.Debug, $"Reading file: ai.prompt", "xSConfig.cs");
                confFile = await sr.ReadToEndAsync();
                AI.SystemInstructions = confFile;
            }
            catch (Exception ex)
            {
                xSLogger.Log(LogLevel.Critical, "Failed to read config files", "xSConfig.cs", ex);
            }
        }

        private static string getValue(string file, string target)
        {
            string pattern = @$"^{target}\s+""([^""]+)"";";

            Match match = Regex.Match(file, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string result = match.Groups[1].Value;
                if (result == "changeMe") throw new Exception($"Target ({target}) is default value. Please change");
                return result;
            }
            else
            {
                throw new Exception($"Target ({target}) couldn't be found in file!");
            }
        }
    }
}