/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace GraphWebhooks_Core
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}                

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ConfigureEndpointDefaults(listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                    // Set properties and call methods on options
                })
                .UseStartup<Startup>();
            }).ConfigureAppConfiguration(configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); });
	}

}
