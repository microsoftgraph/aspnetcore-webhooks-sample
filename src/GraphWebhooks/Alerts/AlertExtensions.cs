// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace GraphWebhooks;

/// <summary>
/// Extension functions for IActionResult that add flash messages.
/// </summary>
public static class AlertExtensions
{
    /// <summary>
    /// Adds error information to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="IActionResult"/> to add information to.</param>
    /// <param name="message">The error message.</param>
    /// <param name="debugInfo">Optional debug information.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    public static IActionResult WithError(
        this IActionResult result,
        string message,
        string? debugInfo = null)
    {
        return Alert(result, "danger", message, debugInfo);
    }

    /// <summary>
    /// Adds success information to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="IActionResult"/> to add information to.</param>
    /// <param name="message">The success message.</param>
    /// <param name="debugInfo">Optional debug information.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    public static IActionResult WithSuccess(
        this IActionResult result,
        string message,
        string? debugInfo = null)
    {
        return Alert(result, "success", message, debugInfo);
    }

    /// <summary>
    /// Adds information to an <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="IActionResult"/> to add information to.</param>
    /// <param name="message">The information message.</param>
    /// <param name="debugInfo">Optional debug information.</param>
    /// <returns>The <see cref="IActionResult"/>.</returns>
    public static IActionResult WithInfo(
        this IActionResult result,
        string message,
        string? debugInfo = null)
    {
        return Alert(result, "info", message, debugInfo);
    }

    private static WithAlertResult Alert(
        IActionResult result,
        string type,
        string message,
        string? debugInfo)
    {
        return new WithAlertResult(result, type, message, debugInfo);
    }
}
