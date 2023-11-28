// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphWebhooks.Areas.MicrosoftIdentity.Pages.Account;

/// <summary>
/// Model for the SignOut page. Overrides the SignedOut.cshtml
/// page exported by Microsoft.Identity.Web.UI
/// to allow redirecting to home page even if not authenticated
/// </summary>
[AllowAnonymous]
public class SignedOutModel : PageModel
{
    /// <summary>
    /// Method handling the HTTP GET method.
    /// </summary>
    /// <returns>Redirect to Home page.</returns>
    public IActionResult OnGet()
    {
        return LocalRedirect("~/");
    }
}
