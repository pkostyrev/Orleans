using Grains.Interfaces.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Orleans.Grains;

public class QueueGrain : Grain, IQueueGrain
{
    private readonly ILogger<QueueGrain> logger;
    private readonly HashSet<Guid> players = new();

    public QueueGrain(ILogger<QueueGrain> logger)
    {
        this.logger = logger;
    }

    private Guid GrainKey => this.GetPrimaryKey();

    public async Task Enter(Guid playerKey)
    {
        if (players.Add(playerKey))
        {
            logger.LogInformation("Palyer {@PlayerKey} enter queue {@QueueKey}",
              playerKey, GrainKey);

            await CreateRoomAttempt();
        }

        await Task.CompletedTask;
    }

    public async Task Exit(Guid playerKey)
    {
        if (players.Remove(playerKey))
        {
            logger.LogInformation("Palyer {@PlayerKey} exit queue {@QueueKey}",
              playerKey, GrainKey);
        }

        await Task.CompletedTask;
    }

    private async Task CreateRoomAttempt()
    {
        if (players.Count >= 2)
        {
            var players = this.players.Take(2);

            await Task.WhenAll([CreateRoom(players.ToImmutableHashSet()), .. players.Select(Exit)]);
        }
    }

    private async Task CreateRoom(ImmutableHashSet<Guid> players)
    {
        IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(Guid.NewGuid());

        await roomGrain.UpdateRoomState(new RoomState(players));
    }
}
