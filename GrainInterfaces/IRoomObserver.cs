using Grains.Interfaces.Models;

namespace Orleans.Grains
{
    public interface IRoomObserver : IGrainObserver
    {
        Task UpdateRoomStatus(StateType type);
    }
}
