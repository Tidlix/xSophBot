using System.Text.Json;
using TwitchSharp.Items;

namespace TwitchSharp.Events.Args
{
    public class FollowReceivedArgs
    {
        private TwitchClient Client { get; set; }
        public TwitchUser Broadcaster { get; private set; }
        public TwitchUser Follower { get; private set; }
        public DateTime FollowedAt { get; private set; }

        public FollowReceivedArgs(TwitchClient Client, JsonElement json)
        {
            this.Client = Client;
            var eventJson = json.GetProperty("payload").GetProperty("event");
            Broadcaster = new TwitchUser(Client, eventJson.GetProperty("broadcaster_user_login").GetString()!);
            Follower = new TwitchUser(Client, eventJson.GetProperty("user_login").GetString()!);
            FollowedAt = eventJson.GetProperty("followed_at").GetDateTime();
        }
    }
}