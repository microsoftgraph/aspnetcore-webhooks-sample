/*
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
 *  See LICENSE in the source repository root for complete license information.
 */

using Microsoft.AspNetCore.Mvc;

namespace GraphWebhooks_Core.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
        
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Error(string message)
        {
            ViewBag.Message = message;
            return View();
        }
    }
}
