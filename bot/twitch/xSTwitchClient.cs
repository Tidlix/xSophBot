using TwitchSharp;
using TwitchSharp.Entitys;
using xSophBot.bot.ai;
using xSophBot.bot.conf;

namespace xSophBot.bot.twitch
{
    public class xSTwitchClient
    {
        public static TwitchClient Client { get; set; }

        public static async Task CreateClientAsync()
        {
            Client = new TwitchClient(xSConfig.Twitch.ClientId, xSConfig.Twitch.ClientSecret, xSConfig.Twitch.RefreshToken);
            Client.SetCurrentUser(new TwitchUser(xSConfig.Twitch.UserName, Client));
            Client.OnMessageReceived += async (s, c, m) =>
            {
                if (m.StartsWith("!ai "))
                    await HandleAiCommandAsync(c, m.Replace("!ai ", ""));
            };
        }

        private static async Task HandleAiCommandAsync(TwitchChannel channel, string request)
        {
            string resonse = await xSGeminiEngine.GenerateResponseAsync(request);
            await channel.SendMessageAsnyc(resonse);
        }
    }
}