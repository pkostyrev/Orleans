using Grains.Interfaces.Models;

namespace Orleans.Grains;

public interface IRoomGrain : IGrainWithGuidKey
{
    Task UpdateRoomState(RoomState roomState);
    Task SendAnswer(Guid playerKey, int answer);
    Task ObserveRoomUpdatesAsync(IRoomObserver observer);
    Task UnobserveRoomUpdatesAsync(IRoomObserver observer);
}
