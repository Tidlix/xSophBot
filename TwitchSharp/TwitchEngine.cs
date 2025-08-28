using Microsoft.Extensions.Logging;
using TwitchSharp.Entitys;
using TwitchSharp.Events;

namespace TwitchSharp
{
    public class TwitchEngine
    {
        public static TwitchSharpLogs Logs = new TwitchSharpLogs();
        public static EventEngine? EventEngine;
#pragma warning disable CS8618
        private static TwitchClient? Client;
#pragma warning restore CS8618 

        #region TwitchClient
        public static TwitchClient CreateTwitchClient(AutomaticTwitchClientConfig conf)
        {
            Client = new TwitchClient(conf.ClientID, conf.ClientSecret, conf.Username, conf.OAuthCode, conf.RedirectUri);
            return Client;
        }
        public static TwitchClient CreateTwitchClient(ManualTwitchClientConfig conf)
        {
            string code = getAuthCode(conf);
            Client = new TwitchClient(conf.ClientID, conf.ClientSecret, conf.Username, code, conf.RedirectUri);
            return Client;
        }
        public static TwitchClient CreateTwitchClient(RefreshTwitchClientConfig conf)
        {
            Client = new TwitchClient(conf.ClientID, conf.ClientSecret, conf.Username, conf.RefreshToken);
            return Client;
        }

        public static EventEngine UseEvents(EventConfig conf)
        {
            EventEngine = new EventEngine(conf);
            return EventEngine;
        }


        public static TwitchClient GetTwitchClient()
        {
            if (Client == null) throw new Exception("Client was not initialized yet! Please use CreateTwitchClient() first!");
            return Client;
        }

        private static string getAuthCode(ManualTwitchClientConfig conf)
        {
            if (conf.Scopes == null) throw new Exception("Scopes cannot be null if OAuthToken is not given!");
            if (conf.RedirectUri == null) throw new Exception("RedirectUri cannot be null if OAuthToken is not given!");

            for (int i = 0; i < conf.Scopes.Length; i++) conf.Scopes[i] = conf.Scopes[i].Replace(":", "%3A");
            string link = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={conf.ClientID}&redirect_uri={conf.RedirectUri}&scope={string.Join('+', conf.Scopes)}";
            Logs.Log("Authorization needed! Please authorize with the following link: \n" + link, LogLevel.Information);

            Console.Write("\nEnter authorization code: ");
            string? code = Console.ReadLine();
            if (code == null) throw new Exception("Authorization code can't be null!");
            return code;
        }
        #endregion
    
    }
}