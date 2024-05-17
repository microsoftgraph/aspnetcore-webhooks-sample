// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace GraphWebhooks.Services;

/// <summary>
/// Implements methods to retrieve certificates from Azure Key Vault.
/// </summary>
public class CertificateService(
    IConfiguration configuration,
    ILogger<CertificateService> logger)
{
    private readonly IConfiguration config = configuration ??
        throw new ArgumentException(nameof(configuration));

    private readonly ILogger<CertificateService> logger = logger ??
        throw new ArgumentException(nameof(logger));

    private byte[]? publicKeyBytes = null;

    private byte[]? privateKeyBytes = null;

    /// <summary>
    /// Gets the configured public key from the Azure Key Vault.
    /// </summary>
    /// <returns>The public key.</returns>
    public async Task<X509Certificate2> GetEncryptionCertificate()
    {
        if (publicKeyBytes == null)
        {
            await LoadCertificates();
        }

        return new X509Certificate2(publicKeyBytes ??
            throw new Exception("Could not load encryption certificate"));
    }

    /// <summary>
    /// Gets the configure private key from the Azure Key Vault.
    /// </summary>
    /// <returns>The private key.</returns>
    public async Task<X509Certificate2> GetDecryptionCertificate()
    {
        if (privateKeyBytes == null)
        {
            await LoadCertificates();
        }

        return new X509Certificate2(privateKeyBytes ??
            throw new Exception("Could not load decryption certificate"));
    }

    /// <summary>
    /// Extract the secret name from the secret ID.
    /// </summary>
    /// <param name="secretId">The URI to the secret.</param>
    /// <returns>The secret name.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the secret ID is invalid.</exception>
    private static string ParseSecretName(Uri secretId)
    {
        // Secret IDs are URIs. The name is in the
        // third segment
        if (secretId.Segments.Length < 3)
        {
            throw new InvalidOperationException($@"The secret ""{secretId}"" does not contain a valid name.");
        }

        return secretId.Segments[2].TrimEnd('/');
    }

    /// <summary>
    /// Gets the public and private keys from Azure Key Vault and caches the raw values.
    /// </summary>
    private async Task LoadCertificates()
    {
        // Load configuration values
        var tenantId = config.GetValue<string>("AzureAd:TenantId");
        var clientId = config.GetValue<string>("AzureAd:ClientId");
        var clientSecret = config.GetValue<string>("AzureAd:ClientSecret");
        var keyVaultUrl = new Uri(config.GetValue<string>("KeyVault:Url") ??
            throw new Exception("KeyVault url not set in appsettings"));
        var certificateName = config.GetValue<string>("KeyVault:CertificateName");

        logger.LogInformation("Loading certificate from Azure Key Vault");

        // Authenticate as the app to connect to Azure Key Vault
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // CertificateClient can get the public key
        var certClient = new CertificateClient(keyVaultUrl, credential);

        // Secret client can get the private key
        var secretClient = new SecretClient(keyVaultUrl, credential);

        // Get the public key
        var publicCertificate = await certClient.GetCertificateAsync(certificateName);

        // Each certificate that has a private key in Azure Key Vault has a corresponding
        // secret ID. Use this to get the private key
        var privateCertificate = await secretClient.GetSecretAsync(ParseSecretName(publicCertificate.Value.SecretId));

        publicKeyBytes = publicCertificate.Value.Cer;
        privateKeyBytes = Convert.FromBase64String(privateCertificate.Value.Value);
    }
}
