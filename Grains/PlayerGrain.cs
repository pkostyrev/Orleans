using Microsoft.Extensions.Logging;
using Orleans.Grains.Models;
using Orleans.Providers;

namespace Orleans.Grains;

[StorageProvider(ProviderName = "PlayerState")]
public class PlayerGrain : Grain<PlayerState>, IPlayerGrain
{
    private readonly ILogger<PlayerGrain> logger;
    private IRoomGrain? currentRoom;
    private string GrainType => nameof(PlayerGrain);
    private Guid GrainKey => this.GetPrimaryKey();

    public PlayerGrain(ILogger<PlayerGrain> logger)
    {
        this.logger = logger;
    }

    #region override
    public override Task OnActivateAsync(CancellationToken token)
    {
        logger.LogInformation("{@GrainType} {@GrainKey} activated.", this.GrainType, this.GrainKey);
        return base.OnActivateAsync(token);
    }
    #endregion

    public async Task<int> GetWinCount() => await Task.FromResult(State.winCount);

    public async Task<IRoomGrain?> GetCurrentRoomAsync() => await Task.FromResult(currentRoom);

    public async Task JoinoRoomAsync(IRoomGrain room)
    {
        currentRoom = room;
        logger.LogInformation("Player {@PlayerKey} joined room {@RoomKey}", GrainKey, room.GetPrimaryKey());
        await Task.CompletedTask;
    }

    public async Task LeaveRoomAsync(IRoomGrain room)
    {
        currentRoom = null;
        logger.LogInformation("Player {@PlayerKey} left room {@RoomKey}", GrainKey, room.GetPrimaryKey());
        await Task.CompletedTask;
    }

    public async Task IncrementWin()
    {
        State.winCount++;
        await WriteStateAsync();
    }
}
