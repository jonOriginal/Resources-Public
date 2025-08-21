using Backend.Api;
using Backend.Api.Services;
using Bot;
using Bot.Services;
using Bot.Workers;
using Common;
using Config.Net;
using Discord.WebSocket;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var config = new ConfigurationBuilder<IBotConfig>()
    .UseEnvironmentVariables()
    .UseCommandLineArgs()
    .Build();

#region Dependencies

builder.Services.AddConfiguration(config);
builder.AddRedisClient(Defaults.ServiceNames.Redis);
builder.Services.AddHttpClient<BackendClient>(
    client => client.BaseAddress = new Uri($"http://{Defaults.ServiceNames.Gateway}")
);
builder.Services.AddSingleton<DiscordSocketClient>();

#endregion

#region Services

builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<ModService>();
builder.Services.AddSingleton<ForumService>();

#endregion

#region Workers

builder.Services.AddHostedService<DiscordWorker>();
builder.Services.AddHostedService<PostSyncWorker>();
builder.Services.AddHostedService<DiscordEventWorker>();
builder.Services.AddHostedService<RedisModEventWorker>();
builder.Services.AddHostedService<RedisPostEventWorker>();

#endregion

var host = builder.Build();

host.Run();