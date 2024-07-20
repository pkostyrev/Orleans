namespace GrainInterfaces;

[GenerateSerializer, Immutable]
public sealed record class PlayerInfo
{
    [Id(0)] public int winСount { get; set; }
}
