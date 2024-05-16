// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net;
using GraphWebhooks.Services;
using GraphWebhooks.SignalR;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace GraphWebhooks;

/// <summary>
/// Startup class to configure ASP.NET middlewares and services.
/// </summary>
/// <param name="configuration">The app configuration.</param>
public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration ??
        throw new ArgumentException(nameof(configuration));

    /// <summary>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// </summary>
    /// <param name="services">The service collection provided by the runtime.</param>
    /// <exception cref="ArgumentException">Thrown if the service collection is null.</exception>
    public void ConfigureServices(IServiceCollection services)
    {
        _ = services ?? throw new ArgumentException("Service collection cannot be null", nameof(services));

        var scopesString = Configuration?.GetValue<string>("GraphScopes") ?? "User.Read";
        var scopesArray = scopesString.Split(' ');
        services

            // Use OpenId authentication
            .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)

            // Specify this is a web app and needs auth code flow
            .AddMicrosoftIdentityWebApp(options =>
            {
                Configuration?.Bind("AzureAd", options);

                options.Prompt = "select_account";

                options.Events.OnAuthenticationFailed = context =>
                {
                    var error = WebUtility.UrlEncode(context.Exception.Message);
                    context.Response
                        .Redirect($"/Home/ErrorWithMessage?message=Authentication+error&debug={error}");
                    context.HandleResponse();

                    return Task.FromResult(0);
                };

                options.Events.OnRemoteFailure = context =>
                {
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
            .EnableTokenAcquisitionToCallDownstreamApi(
                options =>
                {
                    Configuration?.Bind("AzureAd", options);
                },
                scopesArray)

            // Add a GraphServiceClient via dependency injection
            .AddMicrosoftGraph(options =>
            {
                options.Scopes = scopesArray;
            })

            // Use in-memory token cache
            // See https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization
            .AddInMemoryTokenCaches();

        // Add custom services
        services.AddSingleton<SubscriptionStore>();
        services.AddSingleton<CertificateService>();

        // Add SignalR
        services
            .AddSignalR(options => options.EnableDetailedErrors = true)
            .AddJsonProtocol();

        services.AddMvc(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        })

        // Add the Microsoft Identity UI pages for signin/out
        .AddMicrosoftIdentityUI();
    }

    /// <summary>
    /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The application builder provided by the runtime.</param>
    /// <param name="env">The web host environment provided by the runtime.</param>
    /// <exception cref="ArgumentException">Thrown if any parameter is null.</exception>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        _ = app ?? throw new ArgumentException("IApplicationBuilder cannot be null", nameof(app));
        _ = env ?? throw new ArgumentException("IWebHostEnvironment cannot be null", nameof(env));

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
            // Need Razor pages for Microsoft.Identity.Web.UI's pages to work
            endpoints.MapRazorPages();
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Add SignalR notification hub
            endpoints.MapHub<NotificationHub>("/NotificationHub");
        });
    }
}
