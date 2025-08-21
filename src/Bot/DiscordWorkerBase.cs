using Discord;
using Discord.WebSocket;

namespace Bot;

public abstract class DiscordWorkerBase(DiscordSocketClient client) : BackgroundService
{
    protected DiscordSocketClient Client { get; } = client;

    protected abstract Task ExecuteWorkerAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (Client.ConnectionState != ConnectionState.Connected && !stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
        await ExecuteWorkerAsync(stoppingToken);
    }
}