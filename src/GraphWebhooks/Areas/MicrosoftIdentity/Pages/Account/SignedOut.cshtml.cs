// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GraphWebhooks.Areas.MicrosoftIdentity.Pages.Account
{
    /// <summary>
    /// Model for the SignOut page.
    /// </summary>
    [AllowAnonymous]
    public class SignedOutModel : PageModel
    {
        /// <summary>
        /// Method handling the HTTP GET method.
        /// </summary>
        /// <returns>A Sign Out page or Home page.</returns>
        public IActionResult OnGet()
        {
            return LocalRedirect("~/");
        }
    }
}
