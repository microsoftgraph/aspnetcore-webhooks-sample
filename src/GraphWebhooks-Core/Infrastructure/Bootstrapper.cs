using GraphWebhooks_Core.Helpers;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Client;

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
            //            services.Configure<AzureADOptions>(configuration.GetSection("AzureAd"));
            //https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
           // services.AddHttpClient<IGraphApiOperations, GraphApiOperationService>();
        }

        public static void InitializeAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Token acquisition service and its cache implementation
            services.AddAzureAdV2Authentication(configuration)
                    .AddMsal(new string[] { Constants.ScopeMailRead })
                    .AddInMemoryTokenCache()
                   
            /* you could use a cookie based token cache by reaplacing the last
             * trew lines by : .AddCookie().AddCookieBasedTokenCache()  */
            ;

            services.AddMvc();
                
            // Add the sample's SampleAuthProvider and SubscriptionStore.            
            services.AddTransient<ISubscriptionStore, SubscriptionStore>();
            services.AddHttpContextAccessor();
            services.AddSignalR(
                options => options.EnableDetailedErrors = true);
        }
    }
}
