using System.Text.Json;
using TwitchSharp.Entitys;

namespace TwitchSharp.Events.Args
{
    public class PrivateMessageReceivedArgs
    {
        public string MessageContent { get; private set; }
        public string MessageID { get; private set; }
        public TwitchUser Sender { get; private set; }
        public TwitchUser Receiver { get; private set; }

        public PrivateMessageReceivedArgs(JsonElement payload)
        {
            var _Event = payload.GetProperty("event");
            Sender = new TwitchUser(_Event.GetProperty("from_user_login").GetString()!);
            Receiver = new TwitchUser(_Event.GetProperty("to_user_login").GetString()!);
            MessageID = _Event.GetProperty("whisper_id").GetString()!;
            MessageContent = _Event.GetProperty("whisper").GetProperty("text").GetString()!;
        }
    }
}