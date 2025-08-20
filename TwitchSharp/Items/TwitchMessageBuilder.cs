namespace TwitchSharp.Items
{
    public class TwitchMessageBuilder
    {
        private TwitchClient Client;
        public string Content { get; set; }
        public TwitchMessage? RespondToMessage { get; set; }
        public TwitchMessageBuilder(TwitchClient client)
        {
            Client = client;
            Content = "";
            RespondToMessage = null;
        }

        public TwitchMessageBuilder WithContent(string content)
        {
            Content = content;
            return this;
        }
        public TwitchMessageBuilder RespondTo(TwitchMessage message)
        {
            RespondToMessage = message;
            return this;
        }


        public async Task<TwitchMessage> SendAsync(TwitchUser channel)
        {
            if (Content == "") throw new Exception("Failed to send message - Content can't be empty!");
            return await Client.SendMessageAsync(Content, channel, this.RespondToMessage);
        }
    }
}