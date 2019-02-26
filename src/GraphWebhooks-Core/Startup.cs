/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using GraphWebhooks_Core.Extensions;
using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Resource;
using GraphWebhooks_Core.SignalR;
using GraphWebhooks_Core.TokenCacheProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
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
		public Startup(IConfiguration configuration)
		{            
            Configuration = configuration;
		}

        public IConfiguration Configuration { get; }
        		
		private ClaimsPrincipal TransformClaims(ClaimsPrincipal claimsPrincipal)
		{			
			return claimsPrincipal;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            services.AddTokenAcquisition()
                .AddDistributedMemoryCache()
                .AddSession()
                .AddSessionBasedTokenCache();

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                options.Authority = options.Authority + "/v2.0/";
                options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.ForAadInstance(options.Authority).ValidateAadIssuer;
               
                // Response type
                options.ResponseType = "id_token code";
                options.Scope.Add("offline_access");
                options.Scope.Add("Mail.Read");

                // Handling the auth code
                var handler = options.Events.OnAuthorizationCodeReceived;
                options.Events.OnAuthorizationCodeReceived = async context =>
                {
                    var _tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
                    await _tokenAcquisition.AddAccountToCacheFromAuthorizationCode(context, new string[] { "Mail.Read" });
                    await handler(context);
                };

                // Handling the sign-out: removing the account from MSAL.NET cache
                options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                {
                    var user = context.HttpContext.User;

                    // Avoid displaying the select account dialog
                    context.ProtocolMessage.LoginHint = user.GetLoginHint();
                    context.ProtocolMessage.DomainHint = user.GetDomainHint();
                    await Task.FromResult(0);
                };
            });
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

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            // to use a session token cache
            app.UseSession();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
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
