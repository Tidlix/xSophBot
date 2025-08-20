namespace TwitchSharp.Items
{
    public class ClientConfig
    {
        public required string ClientID { get; set; }
        public required string ClientSecret { get; set; }
        public required string Redirect_uri { get; set; }
        public required string Username { get; set; }
        public required string[] Scopes { get; set; }
    }
}