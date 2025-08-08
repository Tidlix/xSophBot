using Microsoft.Extensions.Logging;
using xSophBot.bot.ai;
using xSophBot.bot.conf;
using xSophBot.bot.twitch;

namespace xSophBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            xSConfig.LogLevel = LogLevel.Information;
            await xSConfig.ReadConfigAsync();
            await xSGeminiEngine.StartSession();
            await xSTwitchClient.CreateClientAsync();

            await xSTwitchClient.Client.ConnectToChatAsync(new() { "xsophe" });

            while (true) ;
        }
    }
}