using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TwitchSharp.Entitys
{
    public class TwitchClient
    {
        public string ClientID { get; private set; }
        public string ClientSecret { get; private set; }
        public TwitchUser CurrentUser { get; private set; }
        private string _UserAccessToken { get; set; }
        private string _RefreshToken { get; set; }
        private string _AppAccessToken { get; set; }
        private string _OAuthCode { get; set; }
        private string _RedirectUri { get; set; }


        public event Action<TwitchClient>? OnReady;

        public TwitchClient(string clientID, string clientSecret, string username, string OAuthCode, string redirectUri)
        {
            TwitchEngine.Logs.Log("Initializing TwitchClient with OAuthCode constructor", LogLevel.Trace);
            ClientID = clientID;
            ClientSecret = clientSecret;
            _OAuthCode = OAuthCode;
            _RedirectUri = redirectUri;
            _RefreshToken = "";
            _AppAccessToken = "";
            _UserAccessToken = "";
            CurrentUser = new TwitchUser(this, username);
            TwitchEngine.Logs.Log("TwitchClient initialized with OAuthCode constructor", LogLevel.Debug);
        }
        public TwitchClient(string clientID, string clientSecret, string username, string refreshToken)
        {
            TwitchEngine.Logs.Log("Initializing TwitchClient with RefreshToken constructor", LogLevel.Trace);
            ClientID = clientID;
            ClientSecret = clientSecret;
            _OAuthCode = "";
            _RedirectUri = "";
            _RefreshToken = refreshToken;
            _AppAccessToken = "";
            _UserAccessToken = "";
            CurrentUser = new TwitchUser(this, username);
            TwitchEngine.Logs.Log("TwitchClient initialized with RefreshToken constructor", LogLevel.Debug);
        }

        public async Task StartAsync()
        {
            if (_RefreshToken == "")
                _UserAccessToken = await generateUserTokenAsnyc(_OAuthCode, _RedirectUri);
            else
                _UserAccessToken = await refreshUserTokenAsnyc();

            _AppAccessToken = await generateAppTokenAsnyc();
            
            OnReady?.Invoke(this);
        }
        #region Tokens
        public string GetAppAccessToken()
        {
            if (ValidTokenCheckAsync(_AppAccessToken).Result)
                return _AppAccessToken;
            _AppAccessToken = generateAppTokenAsnyc().Result;
            return _AppAccessToken;
        }
        public string GetUserAccessToken()
        {
            if (ValidTokenCheckAsync(_UserAccessToken).Result)
                return _UserAccessToken;
            return refreshUserTokenAsnyc().Result;
        }

        private async Task<bool> ValidTokenCheckAsync(string token)
        {
            token ??= "null";
            using (var httpClient = new HttpClient())
            {
                string url = "https://id.twitch.tv/oauth2/validate";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
                TwitchEngine.Logs.Log($"Sending validation request to {url} for token {token}", LogLevel.Trace);
                var response = await httpClient.GetAsync(url);
                TwitchEngine.Logs.Log($"Got validation answer for token {token}: {await response.Content.ReadAsStringAsync()}", LogLevel.Trace);

                if (response.IsSuccessStatusCode)
                {
                    TwitchEngine.Logs.Log($"Token {token} is valid!", LogLevel.Debug);
                    return true;
                }
                else
                {
                    TwitchEngine.Logs.Log($"Token {token} is invalid!", LogLevel.Debug);
                    return false;
                }
            }
        }
        private async Task<string> generateAppTokenAsnyc()
        {
            TwitchEngine.Logs.Log("Generating App Access Token", LogLevel.Trace);
            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"grant_type", "client_credentials"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                TwitchEngine.Logs.Log($"Sending App Token request to {destination}", LogLevel.Trace);
                var content = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, content);
                TwitchEngine.Logs.Log($"Got App Token response: {await response.Content.ReadAsStringAsync()}", LogLevel.Trace);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        string token = json.GetProperty("access_token").GetString()!;
                        TwitchEngine.Logs.Log("App Access Token generated successfully", LogLevel.Debug);
                        return token;
                    default:
                        TwitchEngine.Logs.Log($"Failed to generate App Token - Response: {await response.Content.ReadAsStringAsync()}", LogLevel.Debug);
                        throw new Exception($"An Error occured while trying to generate App Access Token - Status code: {response.StatusCode}");
                }
            }
        }
        private async Task<string> generateUserTokenAsnyc(string code, string redirect_uri)
        {
            TwitchEngine.Logs.Log("Generating User Access Token", LogLevel.Trace);
            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"code", code},
                    {"grant_type", "authorization_code"},
                    {"redirect_uri", redirect_uri}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                TwitchEngine.Logs.Log($"Sending User Token request to {destination}", LogLevel.Trace);
                var content = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, content);
                TwitchEngine.Logs.Log($"Got User Token response: {await response.Content.ReadAsStringAsync()}", LogLevel.Trace);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        _UserAccessToken = json.GetProperty("access_token").GetString()!;
                        _RefreshToken = json.GetProperty("refresh_token").GetString()!;
                        TwitchEngine.Logs.Log("User Access Token generated successfully", LogLevel.Debug);
                        return _UserAccessToken;
                    default:
                        TwitchEngine.Logs.Log($"Failed to generate User Token - Response: {await response.Content.ReadAsStringAsync()}", LogLevel.Debug);
                        throw new Exception($"An Error occured while trying to generate User Access Token - Status code: {response.StatusCode}");
                }
            }
        }
        private async Task<string> refreshUserTokenAsnyc()
        {
            TwitchEngine.Logs.Log("Refreshing User Access Token", LogLevel.Trace);
            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{ClientSecret}"},
                    {"grant_type", $"refresh_token"},
                    {"refresh_token", $"{_RefreshToken}"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                TwitchEngine.Logs.Log($"Sending Refresh Token request to {destination}", LogLevel.Trace);
                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, values);
                TwitchEngine.Logs.Log($"Got Refresh Token response: {await response.Content.ReadAsStringAsync()}", LogLevel.Trace);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JsonElement json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
                        _UserAccessToken = json.GetProperty("access_token").GetString()!;
                        _RefreshToken = json.GetProperty("refresh_token").GetString()!;
                        TwitchEngine.Logs.Log("User Access Token refreshed successfully", LogLevel.Debug);
                        return _UserAccessToken;
                    default:
                        TwitchEngine.Logs.Log($"Failed to refresh User Token - Response: {await response.Content.ReadAsStringAsync()}", LogLevel.Debug);
                        throw new Exception($"An Error occured while trying to refresh User Access Token - Status code: {response.StatusCode}");
                }
            }
        }

        #endregion

        public TwitchUser GetTwitchUser(string userLogin)
        {
            return new TwitchUser(this, userLogin);
        }
    }
    #region Configs
    public class BaseTwitchClientConfig
    {
        public required string ClientID { get; set; }
        public required string ClientSecret { get; set; }
        public required string Username { get; set; }
    }
    public class ManualTwitchClientConfig : BaseTwitchClientConfig
    {
        public required string RedirectUri { get; set; }
        public required string[] Scopes { get; set; }
    }
    public class AutomaticTwitchClientConfig : BaseTwitchClientConfig
    {
        public required string RedirectUri { get; set; }
        public required string OAuthCode { get; set; }
    }
    public class RefreshTwitchClientConfig : BaseTwitchClientConfig
    {
        public required string RefreshToken { get; set; }
    }
    #endregion
}
