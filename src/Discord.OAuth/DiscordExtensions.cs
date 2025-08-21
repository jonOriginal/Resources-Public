using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.OAuth;

public static class DiscordAuthenticationOptionsExtensions
{
    public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder)
    {
        return builder.AddDiscord(DiscordDefaults.AuthenticationScheme, _ => { });
    }

    public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder,
        Action<DiscordOptions> configureOptions)
    {
        return builder.AddDiscord(DiscordDefaults.AuthenticationScheme, configureOptions);
    }

    public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, string authenticationScheme,
        Action<DiscordOptions> configureOptions)
    {
        return builder.AddDiscord(authenticationScheme, DiscordDefaults.DisplayName, configureOptions);
    }

    public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, string authenticationScheme,
        string displayName, Action<DiscordOptions> configureOptions)
    {
        return builder.AddOAuth<DiscordOptions, DiscordHandler>(authenticationScheme, displayName,
            configureOptions);
    }
}