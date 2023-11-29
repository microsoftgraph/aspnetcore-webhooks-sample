// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;

namespace GraphWebhooks;

// This extension is to workaround this bug in the SDK:
// https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/2237
// Should no longer be needed after that bug is fixed.
// This is a direct copy of
// https://github.com/microsoftgraph/msgraph-sdk-dotnet-core/blob/dev/src/Microsoft.Graph.Core/Extensions/IDecryptableContentExtensions.cs
public static class ChangeNotificationEncryptedContentExtensions
{
    public static async Task<T?> DecryptAsync<T>(
        this ChangeNotificationEncryptedContent encryptedContent,
        Func<string, string, Task<X509Certificate2>> certificateProvider,
        CancellationToken cancellationToken = default) where T : IParsable, new()
    {
        if (certificateProvider == null)
            throw new ArgumentNullException(nameof(certificateProvider));

        var stringContent = await encryptedContent.DecryptAsync(certificateProvider, cancellationToken)
            .ConfigureAwait(false);
        return KiotaJsonSerializer.Deserialize<T>(stringContent);
    }

    private static async Task<string> DecryptAsync(
        this ChangeNotificationEncryptedContent encryptedContent,
        Func<string, string, Task<X509Certificate2>> certificateProvider,
        CancellationToken cancellationToken = default)
    {
        if (certificateProvider == null)
            throw new ArgumentNullException(nameof(certificateProvider));

        _ = encryptedContent.EncryptionCertificateId ??
            throw new Exception("Certificate ID missing in encrypted content");
        _ = encryptedContent.EncryptionCertificateThumbprint ??
            throw new Exception("Certificate thumbprint missing in encrypted content");
        _ = encryptedContent.DataKey ??
            throw new Exception("Data key missing in encrypted content");
        _ = encryptedContent.Data ??
            throw new Exception("Data missing in encrypted content");

        using var certificate = await certificateProvider(encryptedContent.EncryptionCertificateId, encryptedContent.EncryptionCertificateThumbprint).ConfigureAwait(false);
        using var rsaPrivateKey = certificate.GetRSAPrivateKey() ??
            throw new Exception("Could not get RSA private key from certificate");
        var decryptedSymmetricKey = rsaPrivateKey.Decrypt(Convert.FromBase64String(encryptedContent.DataKey), RSAEncryptionPadding.OaepSHA1);
        using var hashAlg = new HMACSHA256(decryptedSymmetricKey);
        var expectedSignatureValue = Convert.ToBase64String(hashAlg.ComputeHash(Convert.FromBase64String(encryptedContent.Data)));
        if (!string.Equals(encryptedContent.DataSignature, expectedSignatureValue))
        {
            throw new InvalidDataException("Signature does not match");
        }
        else
        {
            return Encoding.UTF8.GetString(
                await AesDecryptAsync(Convert.FromBase64String(encryptedContent.Data), decryptedSymmetricKey, cancellationToken));
        }
    }

    private static async Task<byte[]> AesDecryptAsync(
        byte[] dataToDecrypt,
        byte[] key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            #pragma warning disable SYSLIB0021
            using var cryptoServiceProvider = new AesCryptoServiceProvider
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = key
            };
            #pragma warning restore SYSLIB0021
            var numArray = new byte[16]; //16 is the IV size for the decryption provider required by specification
            Array.Copy(key, numArray, numArray.Length);
            cryptoServiceProvider.IV = numArray;
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
            await cryptoStream.WriteAsync(dataToDecrypt, 0, dataToDecrypt.Length, cancellationToken);
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error occurred while trying to decrypt the input", ex);
        }
    }
}
