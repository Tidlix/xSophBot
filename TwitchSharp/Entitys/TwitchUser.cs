using System.Text.Json;

namespace TwitchSharp.Entitys
{
    public class TwitchUser
    {
        public string Id { get; set; }
        public string LoginName { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string BroadcasterType { get; set; }
        public string Description { get; set; }
        public string ProfileImageUrl { get; set; }
        public string OfflineImageUrl { get; set; }
        public string CreatedAt { get; set; }

        private TwitchClient client { get; set; }

#pragma warning disable CS8618
        internal TwitchUser(string loginName, TwitchClient client)
        {
            LoginName = loginName;
            this.client = client;

            getUserInformationAsync().Wait();

            Type = (Type == "" || Type == null) ? "normal" : Type;
            BroadcasterType = (BroadcasterType == "" || BroadcasterType == null) ? "normal" : BroadcasterType;
            ProfileImageUrl = (ProfileImageUrl == "" || ProfileImageUrl == null) ? "none" : ProfileImageUrl;
            OfflineImageUrl = (OfflineImageUrl == "" || OfflineImageUrl == null) ? "none" : OfflineImageUrl;
        }
#pragma warning restore CS8616

        private async Task getUserInformationAsync()
        {
            using (var httpClient = new HttpClient())
            {
                string destination = $"https://api.twitch.tv/helix/users?login={LoginName}";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await client.getAppTokenAsnyc()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{client.ClientID}");

                var response = await httpClient.GetAsync(destination);
                string content = await response.Content.ReadAsStringAsync();

                JsonElement json = JsonDocument.Parse(content).RootElement;

                try
                {
                    var data = json.GetProperty("data")[0];

                    Id = data.GetProperty("id").GetString()!;
                    LoginName = data.GetProperty("login").GetString()!;
                    DisplayName = data.GetProperty("display_name").GetString()!;
                    Type = data.GetProperty("type").GetString()!;
                    BroadcasterType = data.GetProperty("broadcaster_type").GetString()!;
                    Description = data.GetProperty("description").GetString()!;
                    ProfileImageUrl = data.GetProperty("profile_image_url").GetString()!;
                    OfflineImageUrl = data.GetProperty("offline_image_url").GetString()!;
                    CreatedAt = data.GetProperty("created_at").GetString()!;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    int status = json.GetProperty("status").GetInt16();
                    string message = json.GetProperty("message").GetString()!;
                    throw new Exception($"Couln't get user! {status} - {message}");
                }
            }
        }


        public TwitchChannel GetChannel() => client.GetTwitchChannel(this);
    }
}