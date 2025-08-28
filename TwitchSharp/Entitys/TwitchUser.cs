using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchSharp.Events.Args;
using Websocket.Client;

namespace TwitchSharp.Entitys
{
    public class TwitchUser
    {
        public string ID { get; private set; }
        public string LoginName { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchUserType UserType { get; private set; }
        public TwitchBroadcasterType BroadcasterType { get; private set; }
        public string Description { get; private set; }
        public string ProfileImageUrl { get; private set; }
        public string OfflineImageUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }

        #region Initialization
        public TwitchUser(TwitchClient client, string userName)
        {
            TwitchEngine.Logs.Log($"Initializing TwitchUser for {userName} (client)", LogLevel.Trace);
            var json = getUserByLoginAsync(userName, client).Result;
            var data = json.GetProperty("data")[0];

            ID = data.GetProperty("id").GetString()!;
            LoginName = data.GetProperty("login").GetString()!;
            DisplayName = data.GetProperty("display_name").GetString()!;
            UserType = data.GetProperty("type").GetString() switch
            {
                "admin" => TwitchUserType.Admin,
                "partner " => TwitchUserType.GlobalMod,
                "" or null => TwitchUserType.Normal,
                _ => TwitchUserType.Normal
            };
            BroadcasterType = data.GetProperty("broadcaster_type").GetString() switch
            {
                "affiliate" => TwitchBroadcasterType.Affiliate,
                "partner" => TwitchBroadcasterType.Partner,
                "" or null => TwitchBroadcasterType.Normal,
                _ => TwitchBroadcasterType.Normal
            };
            Description = data.GetProperty("description").GetString()!;
            ProfileImageUrl = data.GetProperty("profile_image_url").GetString()!;
            OfflineImageUrl = data.GetProperty("offline_image_url").GetString()!;
            DateTime.TryParse(data.GetProperty("display_name").GetString()!, out DateTime dt);
            CreatedAt = dt;
            TwitchEngine.Logs.Log($"TwitchUser {LoginName} (client) initialized successfully", LogLevel.Debug);
        }
        public TwitchUser(string userName)
        {
            TwitchEngine.Logs.Log($"Initializing TwitchUser for {userName}", LogLevel.Trace);
            var json = getUserByLoginAsync(userName).Result;
            var data = json.GetProperty("data")[0];

            ID = data.GetProperty("id").GetString()!;
            LoginName = data.GetProperty("login").GetString()!;
            DisplayName = data.GetProperty("display_name").GetString()!;
            UserType = data.GetProperty("type").GetString() switch
            {
                "admin" => TwitchUserType.Admin,
                "partner " => TwitchUserType.GlobalMod,
                "" or null => TwitchUserType.Normal,
                _ => TwitchUserType.Normal
            };
            BroadcasterType = data.GetProperty("broadcaster_type").GetString() switch
            {
                "affiliate" => TwitchBroadcasterType.Affiliate,
                "partner" => TwitchBroadcasterType.Partner,
                "" or null => TwitchBroadcasterType.Normal,
                _ => TwitchBroadcasterType.Normal
            };
            Description = data.GetProperty("description").GetString()!;
            ProfileImageUrl = data.GetProperty("profile_image_url").GetString()!;
            OfflineImageUrl = data.GetProperty("offline_image_url").GetString()!;
            DateTime.TryParse(data.GetProperty("display_name").GetString()!, out DateTime dt);
            CreatedAt = dt;
            TwitchEngine.Logs.Log($"TwitchUser {LoginName} initialized successfully", LogLevel.Debug);
        }

        private async Task<JsonElement> getUserByLoginAsync(string login, TwitchClient? client = null)
        {
            using (var httpClient = new HttpClient())
            {
                string url = $"https://api.twitch.tv/helix/users?login={login}";

                client ??= TwitchEngine.GetTwitchClient();
                var token = client.GetAppAccessToken();

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}"); // GetAppAccessToken = Not set to an Object
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{client.ClientID}");

                TwitchEngine.Logs.Log($"Requesting user data for {login} from {url}", LogLevel.Trace);
                var response = await httpClient.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();
                TwitchEngine.Logs.Log($"Got response for user {login}: {body}", LogLevel.Trace);
                JsonElement json = JsonDocument.Parse(body).RootElement;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        TwitchEngine.Logs.Log($"User data for {login} retrieved successfully", LogLevel.Debug);
                        return json;
                    case HttpStatusCode.BadRequest:
                        TwitchEngine.Logs.Log($"BadRequest while retrieving user {login}: {json.GetProperty("message").GetString()!}", LogLevel.Debug);
                        throw new Exception($"400 - Bad Request! - {json.GetProperty("message").GetString()!}");
                    case HttpStatusCode.Unauthorized:
                        TwitchEngine.Logs.Log($"Unauthorized while retrieving user {login}: {json.GetProperty("message").GetString()!}", LogLevel.Debug);
                        throw new Exception($"401 - Unauthorized! - {json.GetProperty("message").GetString()!}");
                    default:
                        TwitchEngine.Logs.Log($"Unknown error while retrieving user {login}: {json.GetProperty("message").GetString()!}", LogLevel.Debug);
                        throw new Exception($"Unknown Error! - {json.GetProperty("message").GetString()!}");
                }
            }
        }
        #endregion

        public async Task SendMessageAsync(string content, string? replyID = null)
        {
            using (var httpClient = new HttpClient())
            {
                TwitchClient Client = TwitchEngine.GetTwitchClient();

                TwitchEngine.Logs.Log($"Sending message to channel {ID} from {Client.CurrentUser.ID}", LogLevel.Trace);

                string uri = "https://api.twitch.tv/helix/chat/messages";

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.GetUserAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{Client.ClientID}");

                Dictionary<string, string> parameters = new();
                parameters.Add("broadcaster_id", $"{ID}");
                parameters.Add("sender_id", $"{Client.CurrentUser.ID}");
                parameters.Add("message", $"{content}");
                if (replyID != null)
                {
                    TwitchEngine.Logs.Log($"Including reply ID {replyID} in message", LogLevel.Trace);
                    parameters.Add("reply_parent_message_id", replyID);
                }

                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(uri, values);
                string body = await response.Content.ReadAsStringAsync();
                TwitchEngine.Logs.Log($"Got SendMessage response: {body}", LogLevel.Trace);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonElement json = JsonDocument.Parse(body).RootElement;
                    string sended_id = json.GetProperty("data")[0].GetProperty("message_id").GetString()!;
                    TwitchEngine.Logs.Log($"Message sent successfully with ID {sended_id}", LogLevel.Debug);
                }
                else
                {
                    TwitchEngine.Logs.Log($"Failed to send message - Status: {response.StatusCode}, Response: {body}", LogLevel.Debug);
                    throw new Exception($"Couldn't send Message - Status Code {response.StatusCode}");
                }
            }
        }
        public async Task SendWhisperAsync(string content)
        {
            using (var httpClient = new HttpClient())
            {
                TwitchClient Client = TwitchEngine.GetTwitchClient();
                TwitchEngine.Logs.Log($"Sending whiser to channel {ID} from {Client.CurrentUser.ID}", LogLevel.Trace);

                string uri = $"https://api.twitch.tv/helix/whispers?from_user_id={Client.CurrentUser.ID}&to_user_id={ID}";

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.GetUserAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{Client.ClientID}");

                Dictionary<string, string> parameters = new();
                parameters.Add("message", $"{content}");

                var values = new FormUrlEncodedContent(parameters);
                await httpClient.PostAsync(uri, values);
            }
        }
    }
    #region Enums
    public enum TwitchUserType
    {
        Admin,      // Twitch Admin
        GlobalMod,  // Twitch Mod
        Staff,      // Twitch Staff
        Normal      // Not a Twitch member
    }
    public enum TwitchBroadcasterType
    {
        Affiliate,
        Partner,
        Normal
    }
    #endregion
}
