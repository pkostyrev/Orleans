﻿using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseLocalhostClustering()
            .AddMemoryGrainStorage("AccountState");
    })
    .RunConsoleAsync();