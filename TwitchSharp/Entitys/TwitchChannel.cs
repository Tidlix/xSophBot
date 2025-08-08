using System.Text.Json;

namespace TwitchSharp.Entitys
{
    public class TwitchChannel
    {
        public TwitchUser User { get; private set; }
        public string Language { get; private set; }
        public string CurrentGameName { get; private set; }
        public string CurrentGameId { get; private set; }
        public string StreamTitle { get; private set; }
        public int? StreamDelay { get; private set; }
        public string[] Tags { get; private set; }
        public string[] CCLs { get; private set; }
        public bool isBrandedContent { get; private set; }

        private TwitchClient client;

        #region Initialization

#pragma warning disable CS8618
        public TwitchChannel(TwitchUser user, TwitchClient client)
        {
            User = user;
            this.client = client;

            getChannelInformationAsync().Wait();
        }
#pragma warning restore CS8616

        public async Task getChannelInformationAsync()
        {
            using (var httpClient = new HttpClient())
            {
                string destination = $"https://api.twitch.tv/helix/channels?broadcaster_id={User.Id}";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await client.getAppTokenAsnyc()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{client.ClientID}");

                var response = await httpClient.GetAsync(destination);
                string content = await response.Content.ReadAsStringAsync();

                JsonElement json = JsonDocument.Parse(content).RootElement;


                try
                {
                    var data = json.GetProperty("data")[0];

                    Language = data.GetProperty("broadcaster_language").GetString()!;
                    CurrentGameName = data.GetProperty("game_name").GetString()!;
                    CurrentGameId = data.GetProperty("game_id").GetString()!;
                    StreamTitle = data.GetProperty("title").GetString()!;
                    StreamDelay = data.GetProperty("delay").GetInt16();
                    Tags = data.GetProperty("tags")
                        .EnumerateArray()
                        .Select(tag => tag.GetString()!)
                        .ToArray();
                    CCLs = data.GetProperty("content_classification_labels")
                        .EnumerateArray()
                        .Select(label => label.GetString()!)
                        .ToArray();
                    isBrandedContent = data.GetProperty("is_branded_content").GetBoolean(); // line 59
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    int status = json.GetProperty("status").GetInt16();
                    string message = json.GetProperty("message").GetString()!;
                    throw new Exception($"Couln't get channel! {status} - {message}");
                }
            }
        }
        #endregion

        #region Methods
        public async Task SendMessageAsnyc(string message)
        {
            using (var httpClient = new HttpClient())
            {
                string destination = "https://api.twitch.tv/helix/chat/messages";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await client.getUserTokenAsnyc()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{client.ClientID}");

                Dictionary<string, string> parameters = new()
                {
                    {"broadcaster_id", $"{User.Id}"},
                    {"sender_id", $"{client.CurrentUser.Id}"},
                    {"message", $"{message}"}
                };
                var body = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, body);
            }
        }
        #endregion
    }
}