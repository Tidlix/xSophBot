using TwitchSharp;
using TwitchSharp.Events;
using TwitchSharp.Items;
using xSophBot.conf;


namespace xSophBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SConfig.ReadConfigAsync();
            SGeminiEngine.StartSession();
                
            ClientConfig clientConfig = new()
            {
                ClientID = SConfig.Twitch.ClientId,
                ClientSecret = SConfig.Twitch.ClientSecret,
                Redirect_uri = "https://localhost:3000",
                Username = "xsophbot",
                Scopes = [
                    "user:bot",
                    "user:read:chat",
                    "user:write:chat",
                    "chat:edit",
                    "chat:read",
                    "moderator:read:followers"
                ]
            };
            TwitchClient Client = new(clientConfig);
            TwitchUser xSophe = new TwitchUser(Client, "xsophe");


            var msg = new TwitchMessageBuilder(Client);
            msg.WithContent("xSophBot joined the channel");
            await msg.SendAsync(xSophe);

            Client.EventEngine.OnMessageReceived += async (s, e) =>
            {
                try
                {
                    if (e.Message.Content.StartsWith("!ai "))
                    {
                        Console.WriteLine("Got AI request: " + e.Message.Content);
                        string response = await SGeminiEngine.GenerateResponseAsync($"{e.Message.Author.DisplayName} schreibt: {e.Message.Content.Replace("!ai ", "")}");
                        Console.WriteLine("Responding with: " + response);
                        await e.Message.RespondAsync(response);
                    }
                }
                catch (Exception ex)
                {
                    await e.Message.RespondAsync("Ein Fehler ist aufgetreten!");
                    Console.WriteLine(ex);
                }
            };
            Client.EventEngine.OnFollowReceived += async (s, e) =>
            {
                string AImsg = await SGeminiEngine.GenerateResponseAsync($"SYSTEMNACHRICHT > {e.Follower.DisplayName} hat bei {e.Broadcaster.DisplayName} gefollowed. Schreibe eine kleine dankes Nachricht für diesen Nutzer");
                AImsg = AImsg.Contains(e.Follower.DisplayName) ? AImsg : $"@{e.Follower.DisplayName} {AImsg}";
                await e.Broadcaster.SendMessageAsync(AImsg);
            };
            await Client.EventEngine.SubscribeToEventAsnyc(xSophe, new(EventType.MessageReceived));
            await Client.EventEngine.SubscribeToEventAsnyc(xSophe, new (EventType.FollowReceived));

            while (true) ;
        }
    }
}