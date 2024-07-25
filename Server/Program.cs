using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.Title = "Orelans Server";

await Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering()
            .AddMemoryGrainStorage("PlayerState");
    })
    .ConfigureLogging(builder => builder.AddConsole())
    .RunConsoleAsync();
