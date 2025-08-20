using System.Net;
using System.Text.Json;

namespace TwitchSharp.Items
{
    public class TwitchUser
    {
        #region Variables
        private TwitchClient Client { get; set; }
        public string ID { get; private set; }
        public string LoginName { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchUserType UserType { get; private set; }
        public TwitchBroadcasterType BroadcasterType { get; private set; }
        public string Description { get; private set; }
        public string ProfileImageUrl { get; private set; }
        public string OfflineImageUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }
        #endregion

        #region Initialization
#pragma warning disable CS8618
        public TwitchUser(TwitchClient client, string user)
        {
            Client = client;
            /*
            TwitchSharp can get a user by either the user id, or the user login name

            When login name fails, TwitchSharp will assume that an id is given. If both option fails, 
            it will trow an error with inner exception "400 - BadRequest"

            When either login name or id gets an exception "401 - Unauthorized" it will throw an error
            with inner exception "401 - Unauthorized" -> Invalid token
            */
            try
            {
                string response = getUserByLoginAsync(user).Result;
                Convert(response);
            }
            catch (Exception ex)
            {
                if (ex.Message == "400 - Bad Request!")
                {
                    try
                    {
                        string response = getUserByIdAsync(user).Result;
                        Convert(response);
                    }
                    catch (Exception ex2)
                    {
                        if (ex.Message == "400 - Bad Request!")
                            throw new Exception($"Couldn't get User \"{user}\". Invalid User!", ex2);
                        else if (ex.Message == "401 - Unauthorized!")
                            throw new Exception($"Couldn't get User \"{user}\". Invalid Token - Check Client ID and Secret!", ex2);
                        else
                            throw new Exception($"Couldn't get User \"{user}\". Unknown Error!", ex2);
                    }
                }
                if (ex.Message == "401 - Unauthorized!")
                    throw new Exception($"Couldn't get User \"{user}\". Invalid Token - Check Client ID and Secret!", ex);
                else
                    throw new Exception($"Couldn't get User \"{user}\". Unknown Error!", ex);
            }
        }
#pragma warning restore CS8618
        private async Task<string> getUserByLoginAsync(string login)
        {
            using (var httpClient = new HttpClient())
            {
                string url = $"https://api.twitch.tv/helix/users?login={login}";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.GetAppAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{Client.ClientID}");
                var response = await httpClient.GetAsync(url);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
                    case HttpStatusCode.BadRequest:
                        throw new Exception("400 - Bad Request!");
                    case HttpStatusCode.Unauthorized:
                        throw new Exception("401 - Unauthorized!");
                    default:
                        throw new Exception("Unknown Error!");
                }
            }
        }
        private async Task<string> getUserByIdAsync(string id)
        {
            using (var httpClient = new HttpClient())
            {
                string url = $"https://api.twitch.tv/helix/users?id={id}";
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Client.GetAppAccessToken()}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", $"{Client.ClientID}");
                var response = await httpClient.GetAsync(url);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return await response.Content.ReadAsStringAsync();
                    case HttpStatusCode.BadRequest:
                        throw new Exception("400 - Bad Request!");
                    case HttpStatusCode.Unauthorized:
                        throw new Exception("401 - Unauthorized!");
                    default:
                        throw new Exception("Unknown Error!");
                }
            }
        }
        private void Convert(string apiResponse)
        {
            JsonElement json = JsonDocument.Parse(apiResponse).RootElement;
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
        }
        #endregion

        #region Functions/Methods
        public async Task SendMessageAsync(string content)
        {
            await Client.SendMessageAsync(content, this, null);
        }
        #endregion
    }

    #region enums
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