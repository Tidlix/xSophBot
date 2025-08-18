using System.Text;
using System.Text.Json;
using TwitchSharp.Events.Args;
using TwitchSharp.Items;
using Websocket.Client;

namespace TwitchSharp.Events
{
    public class EventEngine
    {
        string sessionID;
        TwitchClient Client;

#pragma warning disable CS8618
        public EventEngine(TwitchClient Client)
#pragma warning restore CS8618
        {
            WebsocketClient ws = new WebsocketClient(new Uri("wss://eventsub.wss.twitch.tv/ws"));
            this.Client = Client;

            ws.MessageReceived.Subscribe(msg =>
            {
                JsonElement json = JsonDocument.Parse(msg.Text!).RootElement;

                try
                {
                    var metadata = json.GetProperty("metadata");

                    string? messageType = metadata.GetProperty("message_type").GetString();
                    if (messageType == "session_welcome")
                    {
                        var payload = json.GetProperty("payload");
                        var session = payload.GetProperty("session");
                        sessionID = session.GetProperty("id").GetString()!;

                        StartListeningForMessagesAsync(Client.CurrentUser).Wait();
                    }
                    else if (messageType == "session_keepalive") return;
                    else HandleEvent(metadata, json);
                    
                }
                catch (Exception ex)
                {
                    throw new Exception("Something went wrong while starting event handler", ex);
                }
            });
            ws.Start().Wait();
        }


        private void HandleEvent(JsonElement meta, JsonElement json)
        {
            string type = meta.GetProperty("subscription_type").GetString()!;

            switch (type)
            {
                case "channel.chat.message":
                    OnMessageReceived.Invoke(Client, new MessageReceivedArgs(Client, json));
                    break;
            }
        }

        public event Action<TwitchClient, MessageReceivedArgs > OnMessageReceived;


        public async Task StartListeningForMessagesAsync(TwitchUser user)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = "https://api.twitch.tv/helix/eventsub/subscriptions";

                    var jsonContent = $@"{{
                                ""type"": ""channel.chat.message"",
                                ""version"": ""1"",
                                ""condition"": {{
                                    ""broadcaster_user_id"": ""{user.ID}"",
                                    ""user_id"": ""{Client.CurrentUser.ID}""
                                }},
                                ""transport"": {{
                                    ""method"": ""websocket"",
                                    ""session_id"": ""{sessionID}""
                                }}
                            }}";


                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("Authorization", $"Bearer {Client.GetUserAccessToken()}");
                    request.Headers.Add("Client-Id", $"{Client.ClientID}");
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    await httpClient.SendAsync(request);
                }
            }
            catch 
            {
                throw;
            }
        }
    }
}