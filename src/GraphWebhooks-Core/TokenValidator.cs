// <copyright file="TokenValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphWebhooks_Core
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;

    public class TokenValidator
    {
        private readonly string TenantId;
        private readonly IEnumerable<string> AppIds;
        private static readonly string issuerPrefix = "https://sts.windows.net/";
        private static readonly string wellKnownUri = "https://login.microsoftonline.com/common/.well-known/openid-configuration";

        private string IssuerToValidate
        {
            get
            {
                return $"{TokenValidator.issuerPrefix}{TenantId}/";
            }
        }
        public TokenValidator(string tenantId, IEnumerable<string> appIds)
        {
            TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            AppIds = appIds ?? throw new ArgumentNullException(nameof(appIds));
        }
        public async Task<bool> ValidateToken(string token)
        {
            ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(TokenValidator.wellKnownUri, new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration openIdConfig = await configurationManager.GetConfigurationAsync();
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = IssuerToValidate,
                    ValidAudiences = AppIds,
                    IssuerSigningKeys = openIdConfig.SigningKeys
                }, out _);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}:{ex.StackTrace}");
                return false;
            }
        }
    }
}
