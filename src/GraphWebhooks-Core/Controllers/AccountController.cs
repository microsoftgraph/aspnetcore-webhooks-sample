/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GraphWebhooks_Core.Infrastructure;
using Microsoft.Identity.Web;

namespace GraphWebhooks_Core.Controllers
{
    public class AccountController : Controller
    {
        private readonly IOptions<MicrosoftIdentityOptions> azureAdOptions;
        private readonly IOptions<AppSettings> appSettings;

        public AccountController(IOptions<AppSettings> appSettings,
                                IOptions<MicrosoftIdentityOptions> azureAdOptions)
        {
            this.appSettings = appSettings;
            this.azureAdOptions = azureAdOptions;
        }               

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
            else if (admin_consent == "True")
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            return View();
        }

        // Redirect to the `adminconsent` endpoint.
        public ActionResult RequestPermissions()
        {
            string redirectUri = System.Net.WebUtility.UrlEncode(appSettings.Value.BaseRedirectUrl + "/Account/GrantPermissions");
            return new RedirectResult(
                $"{ azureAdOptions.Value.Instance}{ azureAdOptions.Value.TenantId }/adminconsent?client_id={ azureAdOptions.Value.ClientId }&redirect_uri={ redirectUri }"
            );
        }        
    }
}
