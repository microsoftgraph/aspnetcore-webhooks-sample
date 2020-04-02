// <copyright file="KeyVaultManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphWebhooks_Core
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Models;

    public class KeyVaultManager
    {
        private string EncryptionCertificate;
        private string DecryptionCertificate;
        private string EncryptionCertificateId;
        private readonly IOptions<KeyVaultOptions> KeyVaultOptions;

        public KeyVaultManager(IOptions<KeyVaultOptions> keyVaultOptions)
        {
            KeyVaultOptions = keyVaultOptions;
        }

        public async Task<string> GetEncryptionCertificate()
        {
            // Always renewing the certificate when creating or renewing the subscription so that the certificate
            // can be rotated/changed in key vault without having to restart the application
            await GetCertificateFromKeyVault().ConfigureAwait(false);
            return EncryptionCertificate;
        }

        public async Task<string> GetDecryptionCertificate()
        {
            if (string.IsNullOrEmpty(DecryptionCertificate))
            {
                await GetCertificateFromKeyVault().ConfigureAwait(false);
            }

            return DecryptionCertificate;
        }

        public async Task<string> GetEncryptionCertificateId()
        {
            if (string.IsNullOrEmpty(EncryptionCertificateId))
            {
                await GetCertificateFromKeyVault().ConfigureAwait(false);
            }

            return EncryptionCertificateId;
        }

        private async Task GetCertificateFromKeyVault()
        {
            try
            {
                string clientId = KeyVaultOptions.Value.ClientId;
                string clientSecret = KeyVaultOptions.Value.ClientSecret;
                string certificateUrl = KeyVaultOptions.Value.CertificateUrl;

                using KeyVaultClient keyVaultClient = new KeyVaultClient(async (authority, resource, scope) =>
                 {
                     ClientCredential adCredential = new ClientCredential(clientId, clientSecret);
                     AuthenticationContext authenticationContext = new AuthenticationContext(authority, null);
                     return (await authenticationContext.AcquireTokenAsync(resource, adCredential)).AccessToken;
                 });
                SecretBundle keyVaultCertificatePfx = await keyVaultClient.GetSecretAsync(certificateUrl).ConfigureAwait(false);
                CertificateBundle keyVaultCertificateCer = await keyVaultClient.GetCertificateAsync(certificateUrl.Replace("/secrets/", "/certificates/", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);

                DecryptionCertificate = keyVaultCertificatePfx.Value;
                EncryptionCertificate = Convert.ToBase64String(keyVaultCertificateCer.Cer);
                EncryptionCertificateId = keyVaultCertificatePfx.SecretIdentifier.Version;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
