using Analysers.Attributes;
using Config.Net;
using ServiceDefaults;

namespace Bot;

[Config]
public interface IBotConfig
{
    [Option(Alias = Defaults.Env.DiscordBotToken)]
    string? DiscordBotToken { get; set; }

    [Option(Alias = Defaults.Env.DiscordGuildId)]
    ulong? GuildId { get; set; }

    [Option(Alias = Defaults.Env.DiscordModForumChannelId)]
    ulong? ForumChannelId { get; set; }

    [Option(Alias = Defaults.Env.DiscordNotificationChannelId)]
    ulong? NotificationChannelId { get; set; }
    
    [Option(Alias = Defaults.Env.DiscordUpdateChannelId)]
    ulong? UpdateChannelId { get; set; }
}