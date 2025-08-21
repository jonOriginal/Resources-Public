using Analysers.Attributes;
using Config.Net;
using ServiceDefaults;

namespace Frontend;

[Config]
public interface IFrontendConfig
{
    [Option(Alias = Defaults.Env.DiscordInviteLink)]
    public string? DiscordInviteLink { get; set; }
}