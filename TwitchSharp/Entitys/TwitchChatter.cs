using System.Text.Json;

namespace TwitchSharp.Entitys
{
    public class TwitchChatter : TwitchUser
    {
        public TwitchUser Broadcaster { get; private set; }
        public bool IsSubscriber { get; private set; } = false;
        public bool IsVIP { get; private set; } = false;
        public bool IsModerator { get; private set; } = false;
        public bool IsBroadcaster { get; private set; } = false;
        public TwitchBadge[] Badges { get; private set; }
        public string HexColor { get; private set; }

        public TwitchChatter(JsonElement messageEventPayload) 
        : base(messageEventPayload.GetProperty("event").GetProperty("chatter_user_login").GetString()!)
        {
            var _Event = messageEventPayload.GetProperty("event");
            Broadcaster = new TwitchUser(_Event.GetProperty("broadcaster_user_login").GetString()!);
            HexColor = _Event.GetProperty("color").GetString()!;

            var badges = _Event.GetProperty("badges").EnumerateArray().ToArray();
            Badges = new TwitchBadge[badges.Count()];
            for (int i = 0; i < badges.Count(); i++)
            {
                string setID = badges[i].GetProperty("set_id").GetString()!;
                string id = badges[i].GetProperty("id").GetString()!;
                string info = badges[i].GetProperty("info").GetString()!;

                if (setID == "subscriber") IsSubscriber = true;
                if (setID == "moderator") IsModerator = true;
                if (setID == "broadcaster") IsBroadcaster = true;
                if (setID == "vip") IsVIP = true;
                Badges[i] = new TwitchBadge(setID, id, info);
            };
        } 
    }
}