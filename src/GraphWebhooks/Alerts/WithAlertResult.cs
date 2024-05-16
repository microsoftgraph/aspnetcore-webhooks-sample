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
public class WithAlertResult(IActionResult result,
                            string type,
                            string message,
                            string? debugInfo) : IActionResult
{
    public IActionResult Result { get; } = result;
    public string Type { get; } = type;
    public string Message { get; } = message;
    public string? DebugInfo { get; } = debugInfo;

    public Task ExecuteResultAsync(ActionContext context)
    {
        _ = context ?? throw new ArgumentException("ActionContext cannot be null", nameof(context));

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
