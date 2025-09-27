using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace xSophBot.conf
{
    public class SConfig
    {
        public static LogLevel LogLevel;

        public static class Twitch
        {
#pragma warning disable CS8618
            public static string ClientId;
            public static string ClientSecret;
            public static string RefreshToken;
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
                string confFile = await sr.ReadToEndAsync();

                Twitch.ClientId = getValue(confFile, "ID");
                Twitch.ClientSecret = getValue(confFile, "Secret");
                Twitch.RefreshToken = getValue(confFile, "Refresh");


                sr = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}/config/ai.conf");
                confFile = await sr.ReadToEndAsync();
                AI.GeminiKey = getValue(confFile, "Gemini_Key");

                sr = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}/config/ai.promt");
                confFile = await sr.ReadToEndAsync();
                AI.SystemInstructions = confFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read config files - {ex.Message}", "SConfig.cs");
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