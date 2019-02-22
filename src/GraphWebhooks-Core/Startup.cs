/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GraphWebhooks_Core
{
	
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			IConfigurationBuilder builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			if (env.IsDevelopment())
			{

				// For details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
			}
			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }
		private ClaimsPrincipal TransformClaims(ClaimsPrincipal claimsPrincipal)
		{			
			return claimsPrincipal;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			// Adds services required for using options.
			services.AddOptions();

			// Register the IConfiguration instance which AppOptions binds against.
			services.Configure<AppSettings>(Configuration);

			// Add framework services.
			services.AddMvc();

			// This sample uses an in-memory cache for tokens and subscriptions. Production apps will typically use some method of persistent storage.
			services.AddMemoryCache();

			// Configure the OWIN pipeline to use cookie auth.
			services.AddAuthentication(SharedOptions =>
			{
				SharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				SharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

			})
				 .AddCookie(
					 options =>
					 {
						 options.LoginPath = "/Account/SignIn/";
						 options.LogoutPath = "/Account/SignOut/";
					 })
				 .AddOpenIdConnect(options =>
				 {
					 options.Authority = Configuration["AADInstance"] + "common";
					 options.ClientSecret = Configuration["AppSecret"];
					 options.ClientId = Configuration["AppId"];
					 options.SaveTokens = true;
					 options.ResponseType = OpenIdConnectResponseType.IdToken;
					 options.SignedOutRedirectUri = Configuration["BaseRedirectUri"] + Configuration["CallbackPath"];
					 options.Events = new OpenIdConnectEvents
					 {
						 OnRemoteFailure = OnAuthenticationFailed,
						 OnAuthenticationFailed = (context) =>
						 {
							 return Task.CompletedTask;
						 },
						 OnTicketReceived = (context) =>
						 {
							 context.Principal = TransformClaims(context.Principal);
							 return Task.CompletedTask;
						 }
						 //OnTokenResponseReceived = (context) =>
						 //{

						 // context.Success();
						 // return Task.CompletedTask;
						 //}
					 };
					 options.TokenValidationParameters = new TokenValidationParameters
					 {
						 ValidateIssuer = false,
						 NameClaimType = "name"
					 };
				 });

			// Add the sample's SampleAuthProvider, SDKHelper, and SubscriptionStore.
			services.AddSingleton<ISampleAuthProvider, SampleAuthProvider>();
			services.AddTransient<ISDKHelper, SDKHelper>();
			services.AddTransient<ISubscriptionStore, SubscriptionStore>();
			services.AddHttpContextAccessor();
			services.AddSignalR(
				options => options.EnableDetailedErrors = true);

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app,
							  IHostingEnvironment env,
							  ILoggerFactory loggerFactory,
							  IMemoryCache cache,
							  IOptions<AppSettings> options)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}


			app.UseStaticFiles();
			app.UseAuthentication();

			app.UseMvc(routes =>
					 {
						 routes.MapRoute(
							 name: "default",
							 template: "{controller=Home}/{action=Index}/{id?}");
					 });

			app.UseSignalR(builder => builder.MapHub<NotificationHub>(new PathString("/notifications")));


		}

		// Handle sign-in errors differently than generic errors.
		private Task OnAuthenticationFailed(RemoteFailureContext context)
		{
			context.HandleResponse();
			context.Response.Redirect("Home/Error?message=" + context.Failure.Message.Replace("\r\n", " "));
			return Task.FromResult(0);
		}
	}
}
