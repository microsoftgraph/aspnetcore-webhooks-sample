// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Claims;
using Microsoft.Graph.Models;

namespace GraphWebhooks;

/// <summary>
/// Extension methods for ClaimsPrincipal to add Graph information
/// to the principal.
/// </summary>
public static class GraphClaimsPrincipalExtensions
{
    /// <summary>
    /// Get the user's display name.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> that contains the user's identity.</param>
    /// <returns>The user's display name.</returns>
    public static string? GetUserGraphDisplayName(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.FindFirstValue(GraphClaimTypes.DisplayName);
    }

    /// <summary>
    /// Get's the user's email address.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> that contains the user's identity.</param>
    /// <returns>The user's email address.</returns>
    public static string? GetUserGraphEmail(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.FindFirstValue(GraphClaimTypes.Email);
    }

    /// <summary>
    /// Adds display name and email address to a ClaimsPrincipal.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> that contains the user's identity.</param>
    /// <param name="user">The <see cref="User"/> object that contains the user's display name and email address.</param>
    public static void AddUserGraphInfo(this ClaimsPrincipal claimsPrincipal, User? user)
    {
        _ = user ?? throw new ArgumentNullException(nameof(user));
        if (claimsPrincipal.Identity is not ClaimsIdentity identity)
        {
            throw new Exception("Could not access identity");
        }

        identity.AddClaim(
            new Claim(GraphClaimTypes.DisplayName, user.DisplayName ?? string.Empty));
        identity.AddClaim(
            new Claim(
                GraphClaimTypes.Email,
                user.Mail ?? user.UserPrincipalName ?? string.Empty));
    }

    /// <summary>
    /// Adds unique user ID and unique tenant ID to a ClaimsPrincipal. This is necessary for
    /// MSAL to extract the user's MSAL ID from the ClaimsPrincipal.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> that contains the user's identity.</param>
    /// <param name="uid">The user's ID.</param>
    /// <param name="utid">The user's tenant ID.</param>
    public static void AddMsalInfo(this ClaimsPrincipal claimsPrincipal, string uid, string utid)
    {
        if (claimsPrincipal.Identity is not ClaimsIdentity identity)
        {
            throw new Exception("Could not access identity");
        }

        identity.AddClaim(new Claim("uid", uid));
        identity.AddClaim(new Claim("utid", utid));
    }
}
