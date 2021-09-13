// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Graph;
using System.Security.Claims;

namespace GraphWebhooks
{
    public static class GraphClaimTypes {
        public const string DisplayName ="graph_name";
        public const string Email = "graph_email";
    }

    /// <summary>
    /// Extension methods for ClaimsPrincipal to add Graph information
    /// to the principal
    /// </summary>
    public static class GraphClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get the user's display name
        /// </summary>
        /// <param name="claimsPrincipal">The ClaimsPrincipal that contains the user's identity</param>
        /// <returns>The user's display name</returns>
        public static string GetUserGraphDisplayName(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.DisplayName);
        }

        /// <summary>
        /// Get's the user's email address
        /// </summary>
        /// <param name="claimsPrincipal">The ClaimsPrincipal that contains the user's identity</param>
        /// <returns>The user's email address</returns>
        public static string GetUserGraphEmail(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.Email);
        }

        /// <summary>
        /// Adds display name and email address to a ClaimsPrincipal
        /// </summary>
        /// <param name="claimsPrincipal">The ClaimsPrincipal that contains the user's identity</param>
        /// <param name="user">The Microsoft.Graph.User object that contains the user's display name and email address</param>
        public static void AddUserGraphInfo(this ClaimsPrincipal claimsPrincipal, User user)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;

            identity.AddClaim(
                new Claim(GraphClaimTypes.DisplayName, user.DisplayName ?? string.Empty));
            identity.AddClaim(
                new Claim(GraphClaimTypes.Email,
                    user.Mail ?? user.UserPrincipalName ?? string.Empty));
        }
    }
}
