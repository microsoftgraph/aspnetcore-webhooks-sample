/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Infrastructure;
using GraphWebhooks_Core.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Client;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GraphWebhooks_Core
{

    public class Startup
	{
		public Startup(IConfiguration configuration)
		{            
            Configuration = configuration;
		}

        public IConfiguration Configuration { get; }
        				
		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{            
            //default system actions and options integration with dependency system
            services.InitializeDefault(Configuration);
            //init AzureAd specific configuration
            services.InitializeAuthentication(Configuration);            
        }
        		
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            
            app.UseSignalR(builder => builder.MapHub<NotificationHub>(new PathString("/NotificationHub")));
        }
	}
}
