// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace GraphWebhooks;

// This extension is to workaround this bug in the SDK:
// https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2237
// Should no longer be needed after that bug is fixed.
public class EncryptableSubscription : Subscription, IEncryptableSubscription {}
