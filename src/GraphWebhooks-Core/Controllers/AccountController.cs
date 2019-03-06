/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Extensions.Configuration;
using GraphWebhooks_Core.Infrastructure;

namespace GraphWebhooks_Core.Controllers
{
    public class AccountController : Controller
    {
        private readonly AzureADOptions azureAdOptions;
        private readonly AppSettings appSettings;

        public AccountController(IOptions<AppSettings> appSettingsAccessor,
                                IConfiguration configuration)
        {
            appSettings = appSettingsAccessor.Value;
            azureAdOptions = new AzureADOptions();
            configuration.Bind("AzureAd", azureAdOptions);            
        }               

        [Authorize]
        // Callback action for the `adminconsent` endpoint.
        public ActionResult GrantPermissions(string admin_consent, string tenant, string error, string error_description)
        {
            // If there was an error getting permissions from the admin, ask for permissions again.
            if (error != null)
            {
                ViewBag.Message = error + ": " + error_description;
                return View("Error");
            }
            // If the admin successfully granted permissions, continue to the Home page.
            else if (admin_consent == "True" && tenant == User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            return View();
        }

        [Authorize]
        // Redirect to the `adminconsent` endpoint.
        public ActionResult RequestPermissions()
        {
            string tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            string redirectUri = System.Net.WebUtility.UrlEncode(appSettings.BaseRedirectUrl + "/Account/GrantPermissions");
            return new RedirectResult(
                $"{ azureAdOptions.Instance}{ tenantId }/adminconsent?client_id={ azureAdOptions.ClientId }&redirect_uri={ redirectUri }"
            );
        }        
    }
}
