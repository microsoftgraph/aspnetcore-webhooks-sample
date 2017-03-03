/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GraphWebhooks_Core.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public async Task SignIn()
        {
            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.Authentication.ChallengeAsync(
                    OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/Home" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Redirect to home page if the user is authenticated.
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            else return new EmptyResult();
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
            else if (admin_consent == "True" && tenant == User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value)
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
            string redirectUri = System.Net.WebUtility.UrlEncode(Startup.BaseRedirectUri + "/Account/GrantPermissions");
            return new RedirectResult(
                $"{ Startup.AADInstance }{ tenantId }/adminconsent?client_id={ Startup.AppId }&redirect_uri={ redirectUri }"
            );
        }

        [HttpGet]
        public async Task EndSession()
        {
            // If AAD sends a single sign-out message to the app, end the user's session, but don't redirect to AAD for sign out.
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
