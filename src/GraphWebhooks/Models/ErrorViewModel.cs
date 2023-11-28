// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace GraphWebhooks.Models;

/// <summary>
/// View model used by the error page
/// </summary>
public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
