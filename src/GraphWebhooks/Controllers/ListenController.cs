// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GraphWebhooks.Models;

namespace GraphWebhooks.Controllers
{
    public class ListenController : Controller
    {
        private readonly ILogger<ListenController> _logger;

        public ListenController(ILogger<ListenController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index([FromQuery]string validationToken = null)
        {
            if (!string.IsNullOrEmpty(validationToken))
            {
                return Ok(validationToken);
            }

            return Accepted();
        }
    }
}
