// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GraphWebhooks;

/// <summary>
/// WithAlertResult adds temporary error/info/success
/// messages to the result of a controller action.
/// This data is read and displayed by the _AlertPartial view
/// </summary>
public class WithAlertResult : IActionResult
{
    public IActionResult Result { get; }
    public string Type { get; }
    public string Message { get; }
    public string? DebugInfo { get; }

    public WithAlertResult(IActionResult result,
                                string type,
                                string message,
                                string? debugInfo)
    {
        Result = result;
        Type = type;
        Message = message;
        DebugInfo = debugInfo;
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        _ = context ?? throw new ArgumentException(nameof(context));

        var factory = context.HttpContext.RequestServices
        .GetService<ITempDataDictionaryFactory>() ??
            throw new Exception("Could not get ITempDataDictionaryFactory");

        var tempData = factory.GetTempData(context.HttpContext);

        tempData["_alertType"] = Type;
        tempData["_alertMessage"] = Message;
        tempData["_alertDebugInfo"] = DebugInfo;

        return Result.ExecuteResultAsync(context);
    }
}
