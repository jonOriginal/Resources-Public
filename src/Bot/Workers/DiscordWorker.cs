using Discord;
using Discord.WebSocket;

namespace Bot.Workers;

public class DiscordWorker(
    DiscordSocketClient client,
    IBotConfig config,
    ILogger<DiscordWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (config.DiscordBotToken == null)
            throw new InvalidOperationException("Discord bot token is not set in the configuration.");

        client.Log += Log;
        await client.LoginAsync(TokenType.Bot, config.DiscordBotToken);
        await client.StartAsync();
        await Task.Delay(-1, stoppingToken);
    }

    private Task Log(LogMessage message)
    {
        logger.LogInformation("[{severity}] {source}: {message}", message.Severity, message.Source, message.Message);
        return Task.CompletedTask;
    }
}