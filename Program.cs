using Microsoft.Extensions.Logging;
using TwitchSharp;
using TwitchSharp.Entitys;
using TwitchSharp.Events;
using xSophBot.conf;

namespace xSophBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SConfig.ReadConfigAsync();
            SGeminiEngine.StartSession();

            LogConfig logConf = new()
            {
                MinimalLogLevel = LogLevel.Information
            };
            TwitchEngine.Logs.ConfigureLogging(logConf);

            ManualTwitchClientConfig clientConf = new()
            {
                ClientID = SConfig.Twitch.ClientId,
                ClientSecret = SConfig.Twitch.ClientSecret,
                RedirectUri = "https://localhost:3000",
                Username = "xsophbot",
                Scopes = [
                    "user:bot",
                    "user:read:chat",
                    "user:write:chat",
                    "user:read:whispers",
                    "user:manage:whispers"
                ]
            };

            TwitchClient Client = TwitchEngine.CreateTwitchClient(clientConf);
            TwitchUser xSophe = new TwitchUser("xsophe");
            Client.OnReady += async (s) =>
            {
                await xSophe.SendMessageAsync("xSophBot is now ready!");
                Console.Clear();
                TwitchEngine.Logs.Log("Client ready and running!", LogLevel.Information);
            };
            await Client.StartAsync();

            EventConfig eventConf = new()
            {
                Client = Client,
                Subscriptions = [
                    new EventSubscription(EventType.ChannelMessageReceived, xSophe),
                    new EventSubscription(EventType.ChannelFollowReceived, xSophe),
                    new EventSubscription(EventType.PrivateMessageReceived)
                ]
            };
            EventEngine Events = TwitchEngine.UseEvents(eventConf);

            Events.OnChannelMessageReceived += async (s, e) =>
            {
                if (e.MessageContent.StartsWith("!ai "))
                {
                    try
                    {
                        TwitchEngine.Logs.Log("Got AI request: " + e.MessageContent, LogLevel.Debug);
                        string response = await SGeminiEngine.GenerateResponseAsync($"{e.Chatter.DisplayName} schreibt: {e.MessageContent.Replace("!ai ", "")}");
                        TwitchEngine.Logs.Log("Responding with: " + response, LogLevel.Debug);
                        await e.Broadcaster.SendMessageAsync(response, e.MessageID);
                    }
                    catch (Exception ex)
                    {
                        await e.Broadcaster.SendMessageAsync("Ein Fehler ist aufgetreten!", e.MessageID);
                        TwitchEngine.Logs.Log("Couldn't respond to AI command", LogLevel.Error, ex);
                    }
                }
            };
            Events.OnChannelFollowReceived += async (s, e) =>
            {
                try
                {
                    TwitchEngine.Logs.Log("Got AI Follower request: ", LogLevel.Debug);
                    string AImsg = await SGeminiEngine.GenerateResponseAsync($"SYSTEMNACHRICHT > {e.Follower.DisplayName} hat bei {e.Broadcaster.DisplayName} gefollowed. Schreibe eine kleine dankes Nachricht für diesen Nutzer");
                    AImsg = AImsg.Contains(e.Follower.DisplayName) ? AImsg : $"@{e.Follower.DisplayName} {AImsg}";
                    TwitchEngine.Logs.Log("Responding with: " + AImsg, LogLevel.Debug);
                    await e.Broadcaster.SendMessageAsync(AImsg);
                }
                catch (Exception ex)
                {
                    await e.Broadcaster.SendMessageAsync("Ein Fehler ist aufgetreten!");
                    TwitchEngine.Logs.Log("Couldn't generate to AI Follow message", LogLevel.Error, ex);
                }

            };
            Events.OnPrivateMessageReceived += async (s, e) =>
            {
                try
                {
                    TwitchEngine.Logs.Log("Got private AI request: " + e.MessageContent, LogLevel.Debug);
                    string response = await SGeminiEngine.GenerateResponseAsync($"{e.Sender.DisplayName} schreibt (privat): {e.MessageContent}");
                    TwitchEngine.Logs.Log("Responding with: " + response, LogLevel.Debug);
                    await e.Sender.SendWhisperAsync(response);
                }
                catch (Exception ex)
                {
                    await e.Sender.SendWhisperAsync("Ein Fehler ist aufgetreten!");
                    TwitchEngine.Logs.Log("Couldn't respond to private AI request", LogLevel.Error, ex);
                }
            };

            await Events.StartListeningAsync();
            while (true) ;
        }
    }
}