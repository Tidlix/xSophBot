using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchSharp.Entitys;

namespace TwitchSharp
{
    public class TwitchClient
    {
        public string ClientID { get; private set; }
        public LogSystem Logging { get; private set; }
        public TwitchUser CurrentUser { get; private set; }

        private string clientSecret { get; set; }
        private string appToken { get; set; }
        private string userToken { get; set; }
        private string refreshToken { get; set; }
        private TcpClient ircClient { get; set; }
        private StreamReader reader { get; set; }
        private StreamWriter writer { get; set; }

        #region Initalization/Tokens

#pragma warning disable CS8618
        public TwitchClient(string client_id, string client_secret, string refresh_token)
        {
            Logging = new LogSystem();
            try
            {
                ClientID = client_id;
                clientSecret = client_secret;
                refreshToken = refresh_token;

                getAppTokenAsnyc().Wait();
                getUserTokenAsnyc().Wait();
            }
            catch (Exception ex)
            {
                Logging.SendMessage(ex.Message, LogLevel.Error);
            }
        }
#pragma warning restore CS8618

        /*
        VALIDATING TOKEN
        - Token has to be validated before every call
        - If token is invalid, the token has to be refreshed
        */
        private async Task<bool> isTokenValid(string token)
        {
            token ??= "null";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
                var response = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                return response.IsSuccessStatusCode;
            }
        }

        /*
        APP ACCESS TOKEN
        - Getting with "Client credentials grant flow" https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#client-credentials-grant-flow
        - Needed for Events
        - Can be used for sending messages in combination with user token 
        */
        public async Task<string> getAppTokenAsnyc()
        {
            if (await isTokenValid(appToken)) return appToken;

            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{clientSecret}"},
                    {"grant_type", "client_credentials"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                var content = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, content);

                string rString = await response.Content.ReadAsStringAsync();
                JsonElement json = JsonDocument.Parse(rString).RootElement;

                try
                {
                    string? at = json.GetProperty("access_token").GetString();
                    if (at == null) throw new Exception(rString);
                    appToken = at;
                    return at;
                }
                catch
                {
                    int status = json.GetProperty("status").GetInt16();
                    string message = json.GetProperty("message").GetString()!;
                    throw new Exception($"Couln't generate app access token! {status} - {message}");
                }

            }
        }

        /*
        USER ACCESS TOKEN
        - Getting with "Authorization code grant flow" https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#authorization-code-grant-flow 
        - Needed for sending messages
        - Probably used for most tasks?
        */
        public async Task<string> getUserTokenAsnyc()
        {
            if (await isTokenValid(userToken)) return userToken;

            using (var httpClient = new HttpClient())
            {
                Dictionary<string, string> parameters = new()
                {
                    {"client_id", $"{ClientID}"},
                    {"client_secret", $"{clientSecret}"},
                    {"grant_type", $"refresh_token"},
                    {"refresh_token", $"{refreshToken}"}
                };

                string destination = "https://id.twitch.tv/oauth2/token";
                var values = new FormUrlEncodedContent(parameters);

                var response = await httpClient.PostAsync(destination, values);

                string rString = await response.Content.ReadAsStringAsync();
                JsonElement json = JsonDocument.Parse(rString).RootElement;

                try
                {
                    string? at = json.GetProperty("access_token").GetString();
                    string? rt = json.GetProperty("refresh_token").GetString();
                    if (at == null) throw new Exception(rString);
                    userToken = at;
                    if (rt == null) throw new Exception(rString);
                    refreshToken = rt;
                    return at;
                }
                catch
                {
                    int status = json.GetProperty("status").GetInt16();
                    string message = json.GetProperty("message").GetString()!;
                    throw new Exception($"Couln't generate user access token! {status} - {message}");
                }
            }
        }
        #endregion

        #region Get stuff
        public TwitchUser GetTwitchUser(string username) => new TwitchUser(username, this);
        public TwitchChannel GetTwitchChannel(TwitchUser user) => new TwitchChannel(user, this);
        public TwitchChannel GetTwitchChannel(string username) => new TwitchChannel(GetTwitchUser(username), this);
        #endregion

        #region Events
        public event Action<TwitchClient, TwitchChannel, string> OnMessageReceived;

        // 🔌 Verbindung zum Twitch-Chat (IRC)
        public async Task ConnectToChatAsync(List<string> channels)
        {
            ircClient = new TcpClient("irc.chat.twitch.tv", 6667);
            var networkStream = ircClient.GetStream();
            reader = new StreamReader(networkStream);
            writer = new StreamWriter(networkStream) { NewLine = "\r\n", AutoFlush = true };

            await writer.WriteLineAsync($"PASS oauth:{userToken}");
            await writer.WriteLineAsync($"NICK {CurrentUser.LoginName}");

            foreach (var channel in channels)
            {
                await writer.WriteLineAsync($"JOIN #{channel}");
            }

            _ = Task.Run(() => ListenForMessages());
        }

        // 📥 Nachrichten lesen und Event feuern
        private async Task ListenForMessages()
        {
            while (ircClient != null && ircClient.Connected)
            {
                var line = await reader!.ReadLineAsync();
                if (line == null) continue;
                Console.WriteLine(line);

                // Twitch Ping-Pong
                if (line.StartsWith("PING"))
                {
                    await writer!.WriteLineAsync("PONG :tmi.twitch.tv");
                    continue;
                }
                // Nachrichten parsen
                if (line.Contains("PRIVMSG"))
                {
                    var parts = line.Split(' ');
                    var channel = parts[2].TrimStart('#');
                    var message = line.Split(" :", 2)[1];

                    OnMessageReceived.Invoke(this, GetTwitchChannel(channel), message);
                }
            }
        }
        #endregion


        #region Config
        public void SetCurrentUser(TwitchUser user) => CurrentUser = user;

        public class LogSystem
        {
            private string dateTimeFormat = "dd.MM.yyyy - HH:mm:ss";
            private LogLevel LogLevel = LogLevel.Information;

            internal void SendMessage(string message, LogLevel level)
            {
                if (level < LogLevel) return;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"[TWITCH-SHARP] ({DateTime.Now.ToString(dateTimeFormat)})");
                switch (level)
                {
                    case LogLevel.Trace:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                    case LogLevel.Information:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                }
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public void ConfigureDateTimeFormat(string format) => dateTimeFormat = format;
            public void ConfigureMinimumLogLevel(LogLevel level) => LogLevel = level;
        }
    }

    #endregion
}