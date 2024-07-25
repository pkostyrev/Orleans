using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Client;
using Orleans.Grains;

Console.Title = "Client";

using IHost host = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(clientBuilder =>
    {
        clientBuilder.UseLocalhostClustering();
    })
    .ConfigureServices(services =>
    {
        services.AddTransient<IRoomObserver, LoggerRoomObserver>()
            .AddSingleton<IHostedService, PlayerHostedService>();
    })
    .UseConsoleLifetime()
    .Build();

await host.StartAsync();

await host.StopAsync();

