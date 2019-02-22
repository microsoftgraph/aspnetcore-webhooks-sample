using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace GraphWebhooks_Core
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args)
		{
			return WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.ConfigureKestrel((context, options) =>
				{
					//options.Limits.MaxRequestHeadersTotalSize = int.MaxValue;
					//options.Limits.MaxRequestHeaderCount = int.MaxValue;
					//options.Limits.MaxRequestBufferSize = int.MaxValue;
					//options.Limits.MaxResponseBufferSize = int.MaxValue;
					
				});
		}
	}

}
