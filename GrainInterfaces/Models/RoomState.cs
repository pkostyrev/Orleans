using System.Collections.Immutable;

namespace Grains.Interfaces.Models;

[GenerateSerializer]
public enum StateType : byte
{
    RoomCreated,
    PlayersConnected,
    AnswersReceived,
    WinnerChosen,
    PlayerDisconnected
}

[GenerateSerializer]
public record class RoomState(
ImmutableHashSet<Guid> PlayerKeys,
StateType StateType = StateType.RoomCreated)
{
    public static RoomState Empty => new RoomState(ImmutableHashSet<Guid>.Empty);

    public RoomState SetState(StateType type) => this with { StateType = type, dirty = true };
    [Id(0)]
    public Dictionary<Guid, int> playersAnswer = new();
    public bool Dirty => dirty;
    [Id(1)]
    private bool dirty = false;

    public void StateProcessed() => dirty = false;
}
