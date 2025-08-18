using TwitchSharp;
using TwitchSharp.Items;
using xSophBot.conf;


namespace xSophBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SConfig.ReadConfigAsync();
            await SGeminiEngine.StartSession();
                
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
                if (e.Message.Content.StartsWith("!ai "))
                {
                    Console.WriteLine("Got AI request: " + e.Message.Content);
                    string response = await SGeminiEngine.GenerateResponseAsync($"{e.Message.Author.DisplayName}: {e.Message.Content}");
                    Console.WriteLine("Responding with: " + response);
                    await e.Message.RespondAsync(response);
                }

            };
            await Client.EventEngine.StartListeningForMessagesAsync(xSophe);

            while (true) ;
        }
    }
}