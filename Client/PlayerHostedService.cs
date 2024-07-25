using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Grains;
using Spectre.Console;

namespace Orleans.Client;

public sealed class PlayerHostedService : IHostedService
{
    private const string MainMenuMessage =
        "[bold fuchsia]/go[/] find an opponent\n" +
        "[bold fuchsia]/exit[/] to exit\n";

    private const string NumRequestMessage = "Enter a number from [underline green]0[/] to [underline green]100[/]: ";

    private readonly ILogger<PlayerHostedService> logger;
    private readonly IClusterClient client;
    private readonly IRoomObserver observer;
    private IQueueGrain queue;
    private IPlayerGrain player;
    private Guid playerId;

    public PlayerHostedService(
        ILogger<PlayerHostedService> logger,
        IClusterClient client,
        IRoomObserver observer)
    {
        this.logger = logger;
        this.client = client;
        this.observer = observer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        queue = client.GetGrain<IQueueGrain>(0);
        playerId = Guid.NewGuid();
        player = client.GetGrain<IPlayerGrain>(playerId);

        SendMainMassage();

        string? input = null;
        do
        {
            input = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("/exit") &&
                AnsiConsole.Confirm("Do you really want to exit?"))
            {
                break;
            }

            if (input.Equals("/go"))
            {
                IRoomGrain? room = await SearchOpponent(cancellationToken);

                if (room is not null)
                {
                    await GameProcess(room, cancellationToken);
                }

                SendMainMassage();
                continue;
            }

        } while (input is not "/exit");
    }

    public async Task<IRoomGrain?> SearchOpponent(CancellationToken cancellationToken)
    {
        await queue.Enter(playerId);

        IRoomGrain? currentRoom = null;

        while (currentRoom is null)
        {
            logger.LogInformation("Getting current room for player {PlayerId}...",
                playerId);

            try
            {
                currentRoom = await player.GetCurrentRoomAsync();
            }
            catch (Exception error)
            {
                logger.LogError(error,
                    "Error while requesting current room for player {PlayerId}",
                    playerId);
            }

            if (currentRoom is null)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1_000), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }


        logger.LogInformation("Observing updates for room {RoomKey}", currentRoom.GetPrimaryKey());

        var reference = client.CreateObjectReference<IRoomObserver>(observer);
        await currentRoom.ObserveRoomUpdatesAsync(reference);

        logger.LogInformation("Subscribed successfully to room {RoomKey}", currentRoom.GetPrimaryKey());

        return await Task.FromResult(currentRoom);
    }

    public async Task GameProcess(IRoomGrain room, CancellationToken cancellationToken)
    {
        var result = AnsiConsole.Ask<int>(NumRequestMessage);

        while (result < 0 || result > 100)
        {
            AnsiConsole.WriteLine("Incorrect number. Try again!");
            result = AnsiConsole.Ask<int>(NumRequestMessage);
        }

        await room.SendAnswer(playerId, result);

        IRoomGrain? currentRoom = await player.GetCurrentRoomAsync();
        while (currentRoom is not null)
        {
            try
            {
                currentRoom = await player.GetCurrentRoomAsync();
            }
            catch (Exception error)
            {
                logger.LogError(error,
                    "Error while requesting current room for player {PlayerId}",
                    playerId);
            }

            if (currentRoom is not null)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1_000), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        logger.LogInformation("Observing updates for room {RoomKey}", room.GetPrimaryKey());

        var reference = client.CreateObjectReference<IRoomObserver>(observer);
        await room.UnobserveRoomUpdatesAsync(reference);

        logger.LogInformation("Subscribed unsubscribe to room {RoomKey}", room.GetPrimaryKey());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await queue.Exit(playerId);
            var reference = client.CreateObjectReference<IRoomObserver>(observer);
            await player.GetCurrentRoomAsync().Result!.UnobserveRoomUpdatesAsync(reference);
        }
        catch (OrleansException error)
        {
            logger.LogWarning(error,
                "Error gracefully removing observer from the active room. Will ignore and continue to shutdown.");
        }
    }

    private void SendMainMassage()
    {
        AnsiConsole.MarkupLine(MainMenuMessage);
        AnsiConsole.WriteLine($"Wins: {player.GetWinCount().Result}");
    }
}