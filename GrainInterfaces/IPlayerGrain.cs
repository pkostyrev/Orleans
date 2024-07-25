namespace Orleans.Grains;

public interface IPlayerGrain : IGrainWithGuidKey
{
    Task<int> GetWinCount();
    Task IncrementWin();
    Task<IRoomGrain?> GetCurrentRoomAsync();
    Task JoinoRoomAsync(IRoomGrain room);
    Task LeaveRoomAsync(IRoomGrain room);
}
