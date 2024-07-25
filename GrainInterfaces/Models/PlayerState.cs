namespace Orleans.Grains.Models;

[GenerateSerializer]
public record class PlayerState()
{
    [Id(0)] public int winCount { get; set; }
}
