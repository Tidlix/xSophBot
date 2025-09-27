﻿using Microsoft.Extensions.Logging;
using TwitchSharp;
using TwitchSharp.Entitys;
using TwitchSharp.Events;
using TwitchSharp.Events.Types;
using xSophBot.conf;

namespace xSophBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await SConfig.ReadConfigAsync();
            SGeminiEngine.StartSession();

            TwitchSharpEngine.ModifyEngine(TwitchSharpEngine.ConsoleLevel.Information, true, true);


            TwitchClientConfig clientConf = new()
            {
                ClientID = SConfig.Twitch.ClientId,
                ClientSecret = SConfig.Twitch.ClientSecret,
                RefreshToken = SConfig.Twitch.RefreshToken,
            };
            TwitchClient client = new TwitchClient(clientConf);
            TwitchEventHandler events = client.UseEvents();

            TwitchUser xSophe = await client.GetUserByLoginAsync("xsophe");

            events.OnChannelStreamOnline += async (s, e) =>
            {
                await e.Broadcaster.SendChatMessageAsync("1.");
            };
            events.OnChannelStreamOffline += async (s, e) =>
            {
                await e.Broadcaster.SendChatMessageAsync("1. (im offline Chat)");
            };
            events.OnClientWhisperReceived += async (s, e) =>
            {
                string response = await SGeminiEngine.GenerateResponseAsync($"{e.Sender.DisplayName} schreibt im Privaten: \"{e.MessageContent}\"");
                await e.Sender.SendWhisperAsync(response);
            };
            events.OnChannelChatMessageReceived += async (s, e) =>
            {
                if (e.MessageContent.StartsWith("!ai ") || e.MessageContent.ToLower().StartsWith($"@{s.CurrentUser.LoginName}"))
                {
                    string response = await SGeminiEngine.GenerateResponseAsync($"{e.Chatter.DisplayName} schreibt bei {e.Broadcaster.DisplayName}: \"{e.MessageContent}\"");
                    await e.Broadcaster.SendChatMessageAsync(response, e.MessageID);
                }
            };
            events.OnChannelFollowReceived += async (s, e) =>
            {
                string msg = await SGeminiEngine.GenerateResponseAsync($"SYSTEM > @{e.Follower.DisplayName} hat gerade ein Follow bei {e.Broadcaster.DisplayName} da gelassen! Schreibe dem Nutzer eine kreative Dankesnachricht!");
                await e.Broadcaster.SendChatMessageAsync(msg);
            };

            await events.SubscribeToEventAsync(new ChannelStreamOnlineEvent(xSophe));
            await events.SubscribeToEventAsync(new ChannelStreamOfflineEvent(xSophe));
            await events.SubscribeToEventAsync(new ChannelFollowReceivedEvent(xSophe));
            await events.SubscribeToEventAsync(new ChannelChatMessageReceivedEvent(xSophe));
            await events.SubscribeToEventAsync(new ClientWhisperReceivedEvent());

            TwitchSharpEngine.SendConsole("Client is now Online!", TwitchSharpEngine.ConsoleLevel.Information);

            while (true) ;
        }
    }
}