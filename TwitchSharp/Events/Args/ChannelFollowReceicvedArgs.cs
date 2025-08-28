using System.Text.Json;
using TwitchSharp.Entitys;

namespace TwitchSharp.Events.Args
{
    public class ChannelFollowReceicvedArgs
    {
        public TwitchUser Broadcaster { get; private set; }
        public TwitchUser Follower { get; private set; }
        public DateTime FollowedAt { get; private set; }

        public ChannelFollowReceicvedArgs(JsonElement payload)
        {
            var _Event = payload.GetProperty("event");

            Broadcaster = new TwitchUser(_Event.GetProperty("broadcaster_user_login").GetString()!);
            Follower = new TwitchUser(_Event.GetProperty("user_login").GetString()!);

            FollowedAt = _Event.GetProperty("followed_at").GetDateTime();
        }
    }
}