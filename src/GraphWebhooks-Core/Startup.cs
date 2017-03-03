/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using GraphWebhooks_Core.Helpers;

namespace GraphWebhooks_Core
{
    public class Startup
    {
        public static string AppId;
        public static string AppSecret;
        public static string AADInstance;
        public static string GraphResourceId;
        public static string BaseRedirectUri;
        public static string NotificationUrl;
        public static IMemoryCache Cache;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // This sample uses an in-memory cache for tokens and subscriptions. Production apps will typically use some method of persistent storage.
            services.AddMemoryCache();
            
            services.AddAuthentication(
                SharedOptions => SharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Add the sample's SampleAuthProvider and SDKHelper implementations.
            services.AddSingleton<ISampleAuthProvider, SampleAuthProvider>();
            services.AddTransient<ISDKHelper, SDKHelper>();
            
            services.AddSignalR(
                options => options.Hubs.EnableDetailedErrors = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
                              IHostingEnvironment env, 
                              ILoggerFactory loggerFactory, 
                              IMemoryCache cache)
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

            // Populate Azure AD configuration values.
            AADInstance = Configuration["Authentication:AzureAd:AADInstance"];
            AppId = Configuration["Authentication:AzureAd:AppId"];
            BaseRedirectUri = Configuration["Authentication:AzureAd:BaseRedirectUri"];

            // Used later to get an access token and create a subscription.
            // This sample uses a password (secret) to authenticate. Production apps should use a certificate.
            AppSecret = Configuration["Authentication:AzureAd:AppSecret"];
            GraphResourceId = Configuration["Authentication:AzureAd:GraphResourceId"]; 
            NotificationUrl = Configuration["NotificationUrl"];
            
            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                Authority = AADInstance + "common/v2.0",
                ClientId = AppId,
                ResponseType = OpenIdConnectResponseType.IdToken,
                PostLogoutRedirectUri = BaseRedirectUri + Configuration["Authentication:AzureAd:CallbackPath"],
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = OnAuthenticationFailed,
                    OnTokenValidated = OnTokenValidated
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    NameClaimType = "name"
                }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSignalR();
        }

        // Custom logic for validating the token.
        private Task OnTokenValidated(TokenValidatedContext context)
        {
            string tenantId = context.Ticket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            // Make sure that the user didn't sign in with a personal Microsoft account.
            if (tenantId == "9188040d-6c67-4c5b-b112-36a304b66dad")
            {
                context.HandleResponse();
                context.Response.Redirect("Home/Error?message=MSA accounts not supported"); // TODO: NOT REDIRECTING
            }
            else
            {
                // Add more issuer validation logic, depending on your scenario.
                // For example, validate that the tenant is in your db of approved tenants.
            }
            return Task.FromResult(0);
        }

        // Handle sign-in errors differently than generic errors.
        private Task OnAuthenticationFailed(FailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("Home/Error?message=" + context.Failure.Message.Replace("\r\n", " "));
            return Task.FromResult(0);
        }
    }
}
