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
                        /* 
                        Session Welcome is a message type, when the connection is established

                        In this Message, the session id will be sendet, which will be used for 
                        subscribe to new Events
                        */
                        var payload = json.GetProperty("payload");
                        var session = payload.GetProperty("session");
                        sessionID = session.GetProperty("id").GetString()!;

                        SubscribeToEventAsnyc(Client.CurrentUser, new (EventType.MessageReceived)).Wait();
                    }
                    else if (messageType == "session_keepalive") return; // Keeps the session alive - no further action needed
                    else HandleEvent(metadata, json);

                }
                catch 
                {
                    throw;
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
                case "channel.follow":
                    OnFollowReceived.Invoke(Client, new FollowReceivedArgs(Client, json));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public event Action<TwitchClient, MessageReceivedArgs> OnMessageReceived;
        public event Action<TwitchClient, FollowReceivedArgs> OnFollowReceived;


        public async Task SubscribeToEventAsnyc(TwitchUser broadcaster, EventSubscription eventSubscription)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = "https://api.twitch.tv/helix/eventsub/subscriptions";

                    var jsonContent = $@"{{
                                ""type"": ""{eventSubscription.Type}"",
                                ""version"": ""{eventSubscription.Version}"",
                                ""condition"": {{
                                    ""broadcaster_user_id"": ""{broadcaster.ID}"",
                                    ""{(eventSubscription.NeedsModerator ? "moderator_user_id" : "user_id")}"": ""{Client.CurrentUser.ID}""
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

                    var response = await httpClient.SendAsync(request);
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
            catch
            {
                throw;
            }
        }
    }
    public class EventSubscription
    {
        public string Type { get; private set; }
        public int Version { get; private set; }
        public bool NeedsModerator { get; private set; }

        public EventSubscription(EventType type)
        {
            switch (type)
            {
                case EventType.MessageReceived:
                    Type = "channel.chat.message";
                    Version = 1;
                    NeedsModerator = false;
                    break;
                case EventType.FollowReceived:
                    Type = "channel.follow";
                    Version = 2;
                    NeedsModerator = true;
                    break;

                default:
                    Type = "Error - Event not Implemented yet";
                    Version = -1;
                    break;
            }
        }
    }
    public enum EventType
    {
        MessageReceived,
        FollowReceived
    }
}