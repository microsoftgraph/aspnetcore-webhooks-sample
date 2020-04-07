// <copyright file="EncryptedContent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphWebhooks_Core.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class defines the Encrypted Content in the Webhook Notification Payload.
    /// </summary>
    public class EncryptedContent
    {
        /// <summary>
        /// Gets or sets the Encrypted Payload Data.
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the HMAC Signature.
        /// </summary>
        [JsonProperty(PropertyName = "dataSignature")]
        public string DataSignature { get; set; }

        /// <summary>
        /// Gets or sets the Encrypted Symmetric Key.
        /// </summary>
        [JsonProperty(PropertyName = "dataKey")]
        public string DataKey { get; set; }

        /// <summary>
        /// Gets or sets the Client's Encryption Certificate ID.
        /// </summary>
        [JsonProperty(PropertyName = "encryptionCertificateId")]
        public string EncryptionCertificateId { get; set; }

        /// <summary>
        /// Gets or sets the Client's Encryption Certificate Thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "encryptionCertificateThumbprint")]
        public string EncryptionCertificateThumbprint { get; set; }
    }
}
