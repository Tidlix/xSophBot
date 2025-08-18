using System.Net;
using System.Text.Json;
using TwitchSharp.Events;
using TwitchSharp.Items;

namespace TwitchSharp
{
    public class TwitchClient
    {
        private string UserAccessToken { get; set; }
        private string AppAccessToken { get; set; }
        private string RefreshToken { get; set; }
        private string Redirect_uri { get; set; }
        private string[] Scopes { get; set; }
        public string ClientID { get; private set; }
        public string ClientSecret { get; private set; }
        public TwitchUser CurrentUser { get; private set; }

        public EventEngine EventEngine { get; private set; }


#pragma warning disable CS8618
        public TwitchClient(ClientConfig config)
        {
            ClientID = config.ClientID;
            ClientSecret = config.ClientSecret;
            Redirect_uri = config.Redirect_uri;
            Scopes = config.Scopes;

            generateAppTokenAsnyc().Wait();
            generateUserTokenAsnyc().Wait();

            CurrentUser = new TwitchUser(this, config.Username);
            EventEngine = new EventEngine(this);
        }
#pragma warning restore CS8618


        #region Tokens
        public string GetAppAccessToken()
        {
            if (ValidTokenCheckAsync(AppAccessToken).Result) return AppAccessToken;
            generateAppTokenAsnyc().Wait();
            return AppAccessToken;
        }
        public string GetUserAccessToken()
        {
            if (ValidTokenCheckAsync(UserAccessToken).Result) return UserAccessToken;
            generateUserTokenAsnyc().Wait();
            return UserAccessToken;
        }

        private async Task<bool> ValidTokenCheckAsync(string token)
        {
            token ??= "null";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
                var response = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                return response.IsSuccessStatusCode;
            }
        }
        public async Task generateAppTokenAsnyc()
        {
            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"grant_type", "client_credentials"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                var content = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, content);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        AppAccessToken = json.GetProperty("access_token").GetString()!;
                        break;
                    default:
                        throw new Exception($"An Error occured while trying to generate App Access Token - Status code: {response.StatusCode}");
                }
            }
        }
        public async Task generateUserTokenAsnyc()
        {
            for (int i = 0; i < Scopes.Length; i++) Scopes[i] = Scopes[i].Replace(":", "%3A");
            string link = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={ClientID}&redirect_uri={Redirect_uri}&scope={string.Join('+', Scopes)}";
            Console.WriteLine($"[TwitchSharp] Authorization needed: {link}");

            Console.Write("Enter authorization code: ");
            string code = Console.ReadLine()!;

            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"code", code},
                    {"grant_type", "authorization_code"},
                    {"redirect_uri", Redirect_uri}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                var content = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, content);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        UserAccessToken = json.GetProperty("access_token").GetString()!;
                        RefreshToken = json.GetProperty("refresh_token").GetString()!;
                        break;
                    default:
                        throw new Exception($"An Error occured while trying to generate User Access Token - Status code: {response.StatusCode}");
                }
            }
        }
        public async Task refreshUserTokenAsnyc()
        {
            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"grant_type", $"refresh_token"},
                    {"refresh_token", $"{RefreshToken}"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, values);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        UserAccessToken = json.GetProperty("access_token").GetString()!;
                        RefreshToken = json.GetProperty("refresh_token").GetString()!;
                        break;
                    default:
                        throw new Exception($"An Error occured while trying to refresh User Access Token - Status code: {response.StatusCode}");
                }
            }
        }
        #endregion

        #region API
        public async Task<TwitchMessage> SendMessageAsync(string content, TwitchUser channel, TwitchMessage? replyTo = null)
        {
            using (var httpClient = new HttpClient())
            {
                string uri = "https://api.twitch.tv/helix/chat/messages";

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetUserAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{ClientID}");

                Dictionary<string, string> parameters = new();
                parameters.Add("broadcaster_id", $"{channel.ID}");
                parameters.Add("sender_id", $"{CurrentUser.ID}");
                parameters.Add("message", $"{content}");
                if (replyTo != null)
                    parameters.Add("reply_parent_message_id", replyTo.ID);

                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(uri, values);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                    string sended_id = json.GetProperty("data")[0].GetProperty("message_id").GetString()!;

                    return new TwitchMessage(this, CurrentUser, channel, content, sended_id);
                }
                else throw new Exception($"Couldn't send Message - Status Code {response.StatusCode}");                
            }
        }
        #endregion
    }
}