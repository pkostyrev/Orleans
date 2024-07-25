using Grains.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Orleans.Grains;

public class RoomGrain : Grain, IRoomGrain
{
    private const int EXCLUDED_MAX_VALUE = 101;
    private static readonly Random random = new Random();

    private readonly ILogger<RoomGrain> logger;
    private readonly HashSet<IRoomObserver> observers = new();
    private RoomState state = RoomState.Empty;
    private IDisposable timer;

    public RoomGrain(ILogger<RoomGrain> logger)
    {
        this.logger = logger;
    }

    #region override
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        timer = this.RegisterGrainTimer(ChekState, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        timer.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
    #endregion

    private Guid GrainKey => this.GetPrimaryKey();

    public async Task SendAnswer(Guid playerKey, int answer)
    {
        if (state.PlayerKeys.Contains(playerKey))
        {
            state.playersAnswer.Add(playerKey, answer);

            logger.LogInformation("Player {@PlayerKey} add answer to room {@RoomKey}", playerKey, GrainKey);
        }

        await Task.CompletedTask;
    }

    public async Task ObserveRoomUpdatesAsync(IRoomObserver observer)
    {
        observers.Add(observer);
        await Task.CompletedTask;
    }

    public async Task UnobserveRoomUpdatesAsync(IRoomObserver observer)
    {
        observers.Remove(observer);
        await Task.CompletedTask;
    }

    public async Task UpdateRoomState(RoomState roomState)
    {
        state = roomState;

        await Notify(state.StateType);

        switch (state.StateType)
        {
            case StateType.RoomCreated:

                await ConnectPlayersAsync();

                break;

            case StateType.PlayersConnected:

                await WaitPlayerAnswersAsync();

                break;

            case StateType.AnswersReceived:

                await ChooseWinnerAsync();

                break;

            case StateType.WinnerChosen:

                await DisconnectPlayersAsync();

                break;

            case StateType.PlayerDisconnected:

                timer.Dispose();
                await Task.CompletedTask;

                break;

            default:
                throw new NotImplementedException();
        }
    }

    private async Task ChekState()
    {
        if (state.Dirty)
        {
            state.StateProcessed();
            await UpdateRoomState(state);
        }
    }

    private async Task ConnectPlayersAsync()
    {
        foreach (var playerKey in state.PlayerKeys)
        {
            try
            {
                await GrainFactory.GetGrain<IPlayerGrain>(playerKey).JoinoRoomAsync(this.AsReference<IRoomGrain>());
            }
            catch (Exception error)
            {
                logger.LogWarning(error, "Failed to tell player {@PlayerKey} to join room {@RoomKey}", playerKey, GrainKey);
            }
        }

        state = state.SetState(StateType.PlayersConnected);
    }

    private async Task WaitPlayerAnswersAsync()
    {
        while (state.playersAnswer.Count != state.PlayerKeys.Count)
        {
            await Task.CompletedTask;
        }

        state = state.SetState(StateType.AnswersReceived);
    }

    private async Task ChooseWinnerAsync()
    {
        int runndomInt = random.Next(EXCLUDED_MAX_VALUE);

        var sortedDifference = state.playersAnswer
            .Select(playerAnswer => new
            {
                difference = Math.Abs(playerAnswer.Value - runndomInt),
                player = playerAnswer.Key
            })
            .OrderBy(a => a.difference);

        var winPlayerTasks = sortedDifference
             .Where(player => player.difference == sortedDifference.First().difference)
             .Select(winPlayer => GrainFactory.GetGrain<IPlayerGrain>(winPlayer.player).IncrementWin());

        await Task.WhenAll(winPlayerTasks);

        state = state.SetState(StateType.WinnerChosen);
    }

    private async Task DisconnectPlayersAsync()
    {
        var disconnectingPlayerTasks = state.PlayerKeys
            .Select(playerKey => new
            {
                playerKey,
                task = GrainFactory.GetGrain<IPlayerGrain>(playerKey)
                .LeaveRoomAsync(this.AsReference<IRoomGrain>())
            });

        foreach (var disconnectingPlayerTask in disconnectingPlayerTasks)
        {
            try
            {
                await disconnectingPlayerTask.task;
            }
            catch (Exception error)
            {
                logger.LogWarning(error, "Failed to tell player {@PlayerKey} to leave the room {@RoomKey}", disconnectingPlayerTask.playerKey, GrainKey);
            }
        }

        state = state.SetState(StateType.PlayerDisconnected);
    }

    private async Task Notify(StateType type)
    {
        List<IRoomObserver> failed = null!;
        foreach (var observer in observers)
        {
            try
            {
                await observer.UpdateRoomStatus(type);
            }
            catch (Exception error)
            {
                logger.LogWarning(error, "Failed to notify observer {@ObserverKey} of state for room {@RoomKey}. Removing observer.", observer.GetPrimaryKey(), GrainKey);

                failed ??= new();
                failed.Add(observer);
            }
        }

        if (failed is not null)
        {
            foreach (var observer in failed)
            {
                observers.Remove(observer);
            }
        }
    }
}
