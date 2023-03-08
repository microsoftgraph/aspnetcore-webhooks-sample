// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GraphWebhooks.Services;

/// <summary>
/// Implements methods to retrieve certificates from Azure Key Vault
/// </summary>
public class CertificateService
{
    private readonly IConfiguration _config;
    private readonly ILogger<CertificateService> _logger;
    private byte[] _publicKeyBytes = null;
    private byte[] _privateKeyBytes = null;

    public CertificateService(
        IConfiguration configuration,
        ILogger<CertificateService> logger)
    {
        _config = configuration ?? throw new ArgumentException(nameof(configuration));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
    }

    /// <summary>
    /// Gets the configured public key from the Azure Key Vault
    /// </summary>
    /// <returns>The public key</returns>
    public async Task<X509Certificate2> GetEncryptionCertificate()
    {
        if (_publicKeyBytes == null)
        {
            await LoadCertificates();
        }

        return new X509Certificate2(_publicKeyBytes);
    }

    /// <summary>
    /// Gets the configure private key from the Azure Key Vault
    /// </summary>
    /// <returns>The private key</returns>
    public async Task<X509Certificate2> GetDecryptionCertificate()
    {
        if (_privateKeyBytes == null)
        {
            await LoadCertificates();
        }

        return new X509Certificate2(_privateKeyBytes);
    }

    /// <summary>
    /// Gets the public and private keys from Azure Key Vault and caches the raw values
    /// </summary>
    private async Task LoadCertificates()
    {
        // Load configuration values
        var tenantId = _config.GetValue<string>("AzureAd:TenantId");
        var clientId = _config.GetValue<string>("AzureAd:ClientId");
        var clientSecret = _config.GetValue<string>("AzureAd:ClientSecret");
        var keyVaultUrl = new Uri(_config.GetValue<string>("KeyVault:Url"));
        var certificateName = _config.GetValue<string>("KeyVault:CertificateName");

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

        _publicKeyBytes = publicCertificate.Value.Cer;
        _privateKeyBytes = Convert.FromBase64String(privateCertificate.Value.Value);
    }

    /// <summary>
    /// Extract the secret name from the secret ID
    /// </summary>
    /// <param name="secretId">The URI to the secret</param>
    /// <returns>The secret name</returns>
    /// <exception cref="InvalidOperationException"></exception>
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
}
