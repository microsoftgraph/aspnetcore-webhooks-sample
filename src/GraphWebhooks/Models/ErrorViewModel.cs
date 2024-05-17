// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// View model used by the error page.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether to show the request ID.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
