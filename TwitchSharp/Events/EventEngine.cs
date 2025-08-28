using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchSharp.Entitys;
using TwitchSharp.Events.Args;
using Websocket.Client;

namespace TwitchSharp.Events
{
    public class EventEngine
    {
        private string? _SessionID = null;
        private TwitchClient _Client;
        private EventSubscription[] _Subscriptions;

        public EventEngine(EventConfig conf)
        {
            _Client = conf.Client;
            _Subscriptions = conf.Subscriptions;
        }

        public async Task StartListeningAsync()
        {
            TwitchEngine.Logs.Log("Initializing Event Listener", LogLevel.Debug);
            // Initialize ws and get sessionID
            WebsocketClient ws = new WebsocketClient(new Uri("wss://eventsub.wss.twitch.tv/ws"));
            ws.MessageReceived.Subscribe(msg =>
            {
                TwitchEngine.Logs.Log($"Got websocked msg: {msg.Text}", LogLevel.Trace);
                JsonElement json = JsonDocument.Parse(msg.Text!).RootElement;
                var metadata = json.GetProperty("metadata");
                var payload = json.GetProperty("payload");

                string messageType = metadata.GetProperty("message_type").GetString()!;
                switch (messageType)
                {
                    case "session_welcome":
                        var session = payload.GetProperty("session");
                        _SessionID = session.GetProperty("id").GetString()!;
                        TwitchEngine.Logs.Log($"Started Websocked connection with id {_SessionID}", LogLevel.Debug);
                        break;
                    case "session_keepalive":
                        TwitchEngine.Logs.Log("Event - Session keepalive", LogLevel.Trace);
                        break;
                    case "notification":
                        string subType = metadata.GetProperty("subscription_type").GetString()!;
                        TwitchEngine.Logs.Log($"Received Event \"{subType}\"", LogLevel.Debug);
                        switch (subType)
                        {
                            case "channel.chat.message":
                                OnChannelMessageReceived?.Invoke(_Client, new ChannelMesssageReceivedArgs(_Client, payload));
                                break;
                            case "channel.follow":
                                OnChannelFollowReceived?.Invoke(_Client, new ChannelFollowReceicvedArgs(payload));
                                break;
                            case "user.whisper.message":
                                OnPrivateMessageReceived?.Invoke(_Client, new PrivateMessageReceivedArgs(payload));
                                break;
                        }
                        break;
                }
            });
            await ws.Start();
            TwitchEngine.Logs.Log("Started Event Listener", LogLevel.Debug);


            // Subscribe to configured events
            using (var httpClient = new HttpClient())
            {
                string url = "https://api.twitch.tv/helix/eventsub/subscriptions";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_Client.GetUserAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{_Client.ClientID}");

                foreach (var sub in _Subscriptions)
                {
                    var jsonContent = $@"{{
                        ""type"": ""{sub.TypeString}"",
                        ""version"": ""{sub.Version}"",
                        ""condition"": {{
                            {(sub.RequiresBroadcaster ? $"\"broadcaster_user_id\": \"{sub.BroadcasterID}\"," : "")}
                            {(sub.RequiresUser ? ((sub.RequiresModRole ? "\"moderator_user_id\"" : "\"user_id\"") + $": \"{_Client.CurrentUser.ID}\"") : "")}
                        }},
                        ""transport"": {{
                            ""method"": ""websocket"",
                            ""session_id"": ""{_SessionID}""
                        }}
                    }}";

                    TwitchEngine.Logs.Log($"Subscribed to event {sub.TypeString}", LogLevel.Debug);

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(url, content);
                }
            }
        }

        public event Action<TwitchClient, ChannelMesssageReceivedArgs>? OnChannelMessageReceived;
        public event Action<TwitchClient, ChannelFollowReceicvedArgs>? OnChannelFollowReceived;
        public event Action<TwitchClient, PrivateMessageReceivedArgs>? OnPrivateMessageReceived;
    }

    public class EventConfig 
    {
        public required TwitchClient Client { get; set; }
        public required EventSubscription[] Subscriptions { get; set; }
    }

    public class EventSubscription 
    {
        public bool RequiresBroadcaster;
        public bool RequiresUser;
        public bool RequiresModRole;
        public string TypeString;
        public string? BroadcasterID = null;
        public int Version;

#pragma warning disable CS8618
        public EventSubscription(EventType type)
        {
            createSubscription(type);
        }
        public EventSubscription(EventType type, TwitchUser channel) 
        {
            createSubscription(type, channel);
        }
#pragma warning restore CS8618
        private void createSubscription(EventType type, TwitchUser? user = null) {
            switch (type)
            {
                case EventType.ChannelMessageReceived:
                    RequiresBroadcaster = true;
                    RequiresUser = true;
                    RequiresModRole = false;
                    TypeString = "channel.chat.message";
                    Version = 1;
                    break;
                case EventType.PrivateMessageReceived:
                    RequiresBroadcaster = false;
                    RequiresUser = true;
                    RequiresModRole = false;
                    TypeString = "user.whisper.message";
                    Version = 1;
                    break;
                case EventType.ChannelFollowReceived:
                    RequiresBroadcaster = true;
                    RequiresUser = true;
                    RequiresModRole = true;
                    TypeString = "channel.follow";
                    Version = 2;
                    break;
            }

            if (RequiresBroadcaster && user == null) throw new Exception($"Event {type} needs a broadcaster but channel is null!");
            if (RequiresBroadcaster && user != null) BroadcasterID = user.ID;
        }
    }
    public enum EventType {
        ChannelMessageReceived,
        PrivateMessageReceived,
        ChannelFollowReceived        
    }
}