/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System;
using System.Security.Cryptography;

namespace GraphWebhooks_Core
{
	public class Program
	{
        const string privateKeyPath = "privkey.pem";
        const string certificatePath = "fullchain.pem";

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
                        if(File.Exists(privateKeyPath) && File.Exists(certificatePath))
                        {
                            var certificate = LoadPemCertificate(certificatePath, privateKeyPath).GetAwaiter().GetResult();
                            listenOptions.UseHttps(certificate);
                        }
                    });
                    // Set properties and call methods on options
                })
                .UseStartup<Startup>();
            }).ConfigureAppConfiguration(configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); });
        public static async Task<X509Certificate2> LoadPemCertificate(string certificatePath, string privateKeyPath)
        {
            using var publicKey = new X509Certificate2(certificatePath);

            var privateKeyText = await File.ReadAllTextAsync(privateKeyPath);
            var privateKeyBlocks = privateKeyText.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var privateKeyBytes = Convert.FromBase64String(privateKeyBlocks[1]);
            using var rsa = RSA.Create();

            if (privateKeyBlocks[0] == "BEGIN PRIVATE KEY")
            {
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            }
            else if (privateKeyBlocks[0] == "BEGIN RSA PRIVATE KEY")
            {
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            }

            using var keyPair = publicKey.CopyWithPrivateKey(rsa);
            return new X509Certificate2(keyPair.Export(X509ContentType.Pfx));
        }
    }
}
