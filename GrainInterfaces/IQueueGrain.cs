
namespace Orleans.Grains;

public interface IQueueGrain : IGrainWithIntegerKey
{
    Task Enter(Guid playerKey);
    Task Exit(Guid playerKey);
}
