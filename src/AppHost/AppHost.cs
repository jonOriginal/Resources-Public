using Projects;
using ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis(Defaults.ServiceNames.Redis);

var discordAppId = builder.AddParameter("discord-app-id");
var discordAppSecret = builder.AddParameter("discord-app-secret", true);
var discordRoleId = builder.AddParameter("discord-role-id");
var discordGuildId = builder.AddParameter("discord-guild-id");
var discordInviteUrl = builder.AddParameter("discord-invite-url");

var cosmosConnectionString = builder.AddParameter("cosmos-connection-string", true);
var cosmosDatabaseName = builder.AddParameter("cosmos-database-name");
var cosmosContainerName = builder.AddParameter("cosmos-container-name");

var tunnelToken = builder.AddParameter("cloudflare-tunnel-token", true);
var tunnelId = builder.AddParameter("cloudflare-tunnel-id");

var discordBotToken = builder.AddParameter("discord-bot-token", true);
var discordModForumChannelId = builder.AddParameter("discord-mod-forum-channel-id");
var notificationChannelId = builder.AddParameter("discord-notification-channel-id");
var discordUpdateChannelId = builder.AddParameter("discord-update-channel-id", true);

var backend = builder.AddProject<Backend>(Defaults.ServiceNames.Gateway)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReplicas(2)
    .WithEnvironment(Defaults.Env.CosmosConnectionString, cosmosConnectionString)
    .WithEnvironment(Defaults.Env.CosmosDatabaseName, cosmosDatabaseName)
    .WithEnvironment(Defaults.Env.CosmosContainerName, cosmosContainerName);

var frontend = builder.AddProject<Frontend>(Defaults.ServiceNames.Frontend)
    .WithReference(backend)
    .WithReference(redis)
    .WaitFor(backend)
    .WaitFor(redis)
    .WithReplicas(2)
    .WithEnvironment(Defaults.Env.DiscordInviteLink, discordInviteUrl);

var dashboard = builder.AddProject<Dashboard>(Defaults.ServiceNames.Dashboard)
    .WithReference(backend)
    .WithReference(redis)
    .WaitFor(backend)
    .WaitFor(redis)
    .WithEnvironment(Defaults.Env.DiscordAppId, discordAppId)
    .WithEnvironment(Defaults.Env.DiscordAppSecret, discordAppSecret)
    .WithEnvironment(Defaults.Env.DiscordAllowedRoleId, discordRoleId)
    .WithEnvironment(Defaults.Env.DiscordGuildId, discordGuildId);

var bot = builder.AddProject<Bot>(Defaults.ServiceNames.DiscordBot)
    .WithReference(backend)
    .WithReference(redis)
    .WaitFor(backend)
    .WaitFor(redis)
    .WithEnvironment(Defaults.Env.DiscordBotToken, discordBotToken)
    .WithEnvironment(Defaults.Env.DiscordModForumChannelId, discordModForumChannelId)
    .WithEnvironment(Defaults.Env.DiscordNotificationChannelId, notificationChannelId)
    .WithEnvironment(Defaults.Env.DiscordUpdateChannelId, discordUpdateChannelId)
    .WithEnvironment(Defaults.Env.DiscordGuildId, discordGuildId);

var cloudflare = builder.AddContainer(Defaults.ServiceNames.Cloudflare, "cloudflare/cloudflared")
    .WithEnvironment(Defaults.Env.CloudflareTunnelToken, tunnelToken)
    .WithEnvironment(Defaults.Env.CloudflareTunnelName, tunnelId)
    .WithArgs("tunnel", "--no-autoupdate", "--loglevel", "debug", "--metrics", "0.0.0.0:2000", "run")
    .WithHttpEndpoint(2000, 2000)
    .WithHttpHealthCheck("/ready", 200);

builder.Build().Run();