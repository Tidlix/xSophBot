using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Websocket.Client;

namespace TwitchSharp.Entitys
{
    public class TwitchUser
    {
        #region Variables / Initialization
        private TwitchClient _Client { get; set; }
        public string ID { get; private set; }
        public string LoginName { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchUserType UserType { get; private set; }
        public TwitchBroadcasterType BroadcasterType { get; private set; }
        public string Description { get; private set; }
        public string ProfileImageUrl { get; private set; }
        public string OfflineImageUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public TwitchUser(TwitchClient client, JsonElement data)
        {
            _Client = client;
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
        }
        #endregion

        #region Sending
        public async Task SendChatMessageAsync(string content, string? replyID = null)
        {
            using (var httpClient = new HttpClient())
            {
                string uri = "https://api.twitch.tv/helix/chat/messages";

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _Client.GetUserAccessTokenAsync()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{_Client.ClientID}");

                Dictionary<string, string> parameters = new();
                parameters.Add("broadcaster_id", $"{ID}");
                parameters.Add("sender_id", $"{_Client.CurrentUser.ID}");
                parameters.Add("message", $"{content}");
                if (replyID != null)
                {
                    parameters.Add("reply_parent_message_id", replyID);
                }

                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(uri, values);
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonElement json = JsonDocument.Parse(body).RootElement;
                    string sended_id = json.GetProperty("data")[0].GetProperty("message_id").GetString()!;
                }
                else
                {
                    throw new Exception($"Couldn't send Message - Status Code {response.StatusCode}");
                }
            }
        }
        public async Task SendWhisperAsync(string content)
        {
            using (var httpClient = new HttpClient())
            {
                string uri = $"https://api.twitch.tv/helix/whispers?from_user_id={_Client.CurrentUser.ID}&to_user_id={ID}";

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _Client.GetUserAccessTokenAsync()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{_Client.ClientID}");

                Dictionary<string, string> parameters = new();
                parameters.Add("message", $"{content}");

                var values = new FormUrlEncodedContent(parameters);
                await httpClient.PostAsync(uri, values);
            }
        }
        #endregion

        #region Getting
        public TwitchStream? GetCurrentStream()
        {
            TwitchStream? result = null;
            try { result = new TwitchStream(_Client, this); } catch {}
            return result;
        }
        #endregion
    }
    #region Enums
    public enum TwitchUserType
    {
        Admin,      // Twitch Admin
        GlobalMod,  // Twitch Mod
        Staff,      // Twitch Staff
        Normal      // Twitch User
    }
    public enum TwitchBroadcasterType
    {
        Affiliate,
        Partner,
        Normal
    }
    #endregion
}
