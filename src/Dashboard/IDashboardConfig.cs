using Analysers.Attributes;
using Config.Net;
using ServiceDefaults;

namespace Dashboard;

[Config]
public interface IDashboardConfig
{
    [Option(Alias = Defaults.Env.DiscordAppId)]
    string? DiscordAppId { get; set; }

    [Option(Alias = Defaults.Env.DiscordAppSecret)]
    string? DiscordAppSecret { get; set; }

    [Option(Alias = Defaults.Env.DiscordAllowedRoleId)]
    ulong? DiscordRoleId { get; set; }

    [Option(Alias = Defaults.Env.DiscordGuildId)]
    ulong? DiscordGuildId { get; set; }
}