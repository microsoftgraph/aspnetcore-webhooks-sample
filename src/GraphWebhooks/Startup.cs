// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphWebhooks.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace GraphWebhooks
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
            var scopesString = Configuration.GetValue<string>("GraphScopes") ?? "User.Read";
            var scopesArray = scopesString.Split(' ');
            services
                // Use OpenId authentication
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                // Specify this is a web app and needs auth code flow
                .AddMicrosoftIdentityWebApp(options => {
                    Configuration.Bind("AzureAd", options);

                    options.Prompt = "select_account";

                    options.Events.OnAuthenticationFailed = context => {
                        var error = WebUtility.UrlEncode(context.Exception.Message);
                        context.Response
                            .Redirect($"/Home/ErrorWithMessage?message=Authentication+error&debug={error}");
                        context.HandleResponse();

                        return Task.FromResult(0);
                    };

                    options.Events.OnRemoteFailure = context => {
                        if (context.Failure is OpenIdConnectProtocolException)
                        {
                            var error = WebUtility.UrlEncode(context.Failure.Message);
                            context.Response
                                .Redirect($"/Home/ErrorWithMessage?message=Sign+in+error&debug={error}");
                            context.HandleResponse();
                        }

                        return Task.FromResult(0);
                    };
                })
                // Add ability to call web API (Graph)
                // and get access tokens
                .EnableTokenAcquisitionToCallDownstreamApi(options => {
                    Configuration.Bind("AzureAd", options);
                }, scopesArray)
                // Add a GraphServiceClient via dependency injection
                .AddMicrosoftGraph(options => {
                    options.Scopes = scopesString;
                })
                // Use in-memory token cache
                // See https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization
                .AddInMemoryTokenCaches();

            services.AddSingleton<SubscriptionStore>();

            services.AddMvc(options => {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            // Add the Microsoft Identity UI pages for signin/out
            .AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
