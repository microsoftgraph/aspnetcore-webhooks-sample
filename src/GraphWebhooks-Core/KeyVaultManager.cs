// <copyright file="KeyVaultManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphWebhooks_Core
{
    using System;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Azure.Security.KeyVault.Certificates;
    using Microsoft.Extensions.Options;
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
                string keyVaultUri = KeyVaultOptions.Value.KeyVaultUri;

                var keyVaultSecretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                var certificateClient = new CertificateClient(new Uri(keyVaultUri), new DefaultAzureCredential());

                KeyVaultSecret keyVaultCertificatePfx = await keyVaultSecretClient.GetSecretAsync(certificateUrl.Replace("/certificates/", "/secrets/", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                KeyVaultCertificate keyVaultCertificateCer = await certificateClient.GetCertificateVersionAsync(certificateUrl.Replace("/secrets/", "/certificates/", StringComparison.OrdinalIgnoreCase), keyVaultCertificatePfx.Properties.Version).ConfigureAwait(false);

                DecryptionCertificate = keyVaultCertificatePfx.Value;
                EncryptionCertificate = Convert.ToBase64String(keyVaultCertificateCer.Cer);
                EncryptionCertificateId = keyVaultCertificatePfx.Properties.Version;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
