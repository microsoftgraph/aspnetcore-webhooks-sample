// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using GraphWebhooks.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GraphWebhooks.Controllers;

/// <summary>
/// The controller for the home page.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    public HomeController()
    {
    }

    /// <summary>
    /// GET /.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// GET /error.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/>.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
