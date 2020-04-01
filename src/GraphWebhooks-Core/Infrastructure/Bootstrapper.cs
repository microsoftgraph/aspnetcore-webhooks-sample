/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using GraphWebhooks_Core.Helpers;
using GraphWebhooks_Core.Helpers.Interfaces;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;

namespace GraphWebhooks_Core.Infrastructure
{
    public static class Bootstrapper
    {
        public static void InitializeDefault(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            // Register the IConfiguration instance which AppOptions binds against.
            services.Configure<AppSettings>(configuration);       
            //https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        }

        public static void InitializeAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Token acquisition service and its cache implementation
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddSignIn(configuration);
            services.AddWebAppCallsProtectedWebApi(configuration, new string[] { Constants.ScopeMailRead })
                .AddInMemoryTokenCaches();

            services.AddTransient<ISubscriptionStore, SubscriptionStore>();
            services.AddHttpContextAccessor();
            services.AddSignalR(
                options => options.EnableDetailedErrors = true);
            services.AddMvc()
                .AddMicrosoftIdentityUI();
        }
    }
}
