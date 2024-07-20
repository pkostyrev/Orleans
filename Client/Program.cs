using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(clientBuilder =>
    {
        clientBuilder.UseLocalhostClustering();
    })
    .UseConsoleLifetime()
    .Build();

await host.StartAsync();