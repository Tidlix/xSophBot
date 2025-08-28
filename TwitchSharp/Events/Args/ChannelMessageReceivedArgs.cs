using System.Text.Json;
using TwitchSharp.Entitys;

namespace TwitchSharp.Events.Args
{
    public class ChannelMesssageReceivedArgs
    {
        public string MessageContent { get; private set; }
        public string MessageID { get; private set; }
        public TwitchUser Broadcaster { get; private set; }
        public TwitchChatter Chatter { get; private set; }
        
        public ChannelMesssageReceivedArgs(TwitchClient Client, JsonElement payload)
        {
            Chatter = new TwitchChatter(payload);
            Broadcaster = Chatter.Broadcaster;

            var _Event = payload.GetProperty("event");
            MessageID = _Event.GetProperty("message_id").GetString()!;
            var _Message = _Event.GetProperty("message");
            MessageContent = _Message.GetProperty("text").GetString()!;
        }
    }
}