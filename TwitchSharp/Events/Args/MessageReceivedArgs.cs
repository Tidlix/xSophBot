using System.Text.Json;
using TwitchSharp.Items;

namespace TwitchSharp.Events.Args
{
  public class MessageReceivedArgs
  {
    private TwitchClient Client { get; set; }
    public TwitchMessage Message { get; private set; }
    public DateTime MessageSendetAt { get; private set; }

    public MessageReceivedArgs(TwitchClient Client, JsonElement json)
    {
      this.Client = Client;

      var eventJson = json.GetProperty("payload").GetProperty("event");
      TwitchUser broadcaster = new TwitchUser(Client, eventJson.GetProperty("broadcaster_user_login").GetString()!);
      TwitchUser sender = new TwitchUser(Client, eventJson.GetProperty("chatter_user_login").GetString()!);

      var messageJson = eventJson.GetProperty("message");
      string content = messageJson.GetProperty("text").GetString()!;
      string messageID = eventJson.GetProperty("message_id").GetString()!;
      Message = new TwitchMessage(Client, sender, broadcaster, content, messageID);

      MessageSendetAt = DateTime.Now;
    }

    /*
    {
{
  "metadata": {
    "message_id": "XKqCWJpKFvWBZBVWgo4_wmeTsBGGvmjwZLwbiI1xHRs=",
    "message_type": "notification",
    "message_timestamp": "2025-08-17T20:54:52.122361111Z",
    "subscription_type": "channel.chat.message",
    "subscription_version": "1"
  },
  "payload": {
    "subscription": {
      "id": "a2ef72f3-7f8b-4712-9c0b-bf53e8cfd2d3",
      "status": "enabled",
      "type": "channel.chat.message",
      "version": "1",
      "condition": {
        "broadcaster_user_id": "869914383",
        "user_id": "1347315715"
      },
      "transport": {
        "method": "websocket",
        "session_id": "AgoQqs65ZzufRfCEU8xbiKpgXhIGY2VsbC1j"
      },
      "created_at": "2025-08-17T20:54:35.674429447Z",
      "cost": 0
    },
    "event": {
      "broadcaster_user_id": "869914383",
      "broadcaster_user_login": "xsophe",
      "broadcaster_user_name": "xSophe",
      "source_broadcaster_user_id": null,
      "source_broadcaster_user_login": null,
      "source_broadcaster_user_name": null,
      "chatter_user_id": "571743364",
      "chatter_user_login": "glow_jamesbond_007",
      "chatter_user_name": "glow_jamesbond_007",
      "message_id": "b7f16a65-4224-488c-8001-f2d5cafe8592",
      "source_message_id": null,
      "is_source_only": null,
      "message": {
        "text": "woo warst du die letzten 9 monate",
        "fragments": [
          {
            "type": "text",
            "text": "woo warst du die letzten 9 monate",
            "cheermote": null,
            "emote": null,
            "mention": null
          }
        ]
      },
      "color": "#DAA520",
      "badges": [
        {
          "set_id": "hype-train",
          "id": "2",
          "info": ""
        }
      ],
      "source_badges": null,
      "message_type": "text",
      "cheer": null,
      "reply": null,
      "channel_points_custom_reward_id": null,
      "channel_points_animation_id": null
    }
  }
}

    */
  }
}