using Grains.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans.Grains;

namespace Orleans.Client
{
    public class LoggerRoomObserver : IRoomObserver
    {
        private readonly ILogger<LoggerRoomObserver> logger;

        public LoggerRoomObserver(ILogger<LoggerRoomObserver> logger)
        {
            this.logger = logger;
        }

        public Task UpdateRoomStatus(StateType type)
        {
            logger.LogInformation("Update room status: {@StatusType}", type);
            return Task.CompletedTask;
        }
    }
}
