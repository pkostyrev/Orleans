using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Grains;

[Reentrant]
public class PlayerGrain : Grain, IPlayerGrain
{
    private readonly ILogger<PlayerGrain> _logger;
    private readonly IPersistentState<PlayerInfo> _state;

    public PlayerGrain(
       [PersistentState(stateName: "playerInfo", storageName: "Players")] IPersistentState<PlayerInfo> state,
       ILogger<PlayerGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    private static string GrainType => nameof(PlayerGrain);
    private string GrainKey => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken _)
    {
        _logger.LogInformation("{GrainType} {GrainKey} activated.", GrainType, GrainKey);

        return Task.CompletedTask;
    }
}
