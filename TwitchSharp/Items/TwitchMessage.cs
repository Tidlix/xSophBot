namespace TwitchSharp.Items
{
    public class TwitchMessage
    {
        private TwitchClient Client;
        public TwitchUser Author { get; private set; }
        public TwitchUser Channel { get; private set; }
        public string Content { get; private set; }
        public string ID { get; private set; }

        public TwitchMessage(TwitchClient client, TwitchUser author, TwitchUser channel, string content, string id)
        {
            Client = client;
            Author = author;
            Channel = channel;
            Content = content;
            ID = id;
        }

        public async Task<TwitchMessage> RespondAsync(string content)
        {
            if (Content == "") throw new Exception("Failed to send message - Content can't be empty!");
            return await Client.SendMessageAsync(content, Channel, this);
        }
    }
}