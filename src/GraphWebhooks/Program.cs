// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace GraphWebhooks;

/// <summary>
/// Contains the main entry point of the app.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point of the app.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
