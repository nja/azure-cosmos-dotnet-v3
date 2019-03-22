﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Azure.Cosmos.Internal;
    using Newtonsoft.Json;

    [Serializable]
    internal sealed class DocumentServiceLeaseCore : DocumentServiceLease
    {
        private static readonly DateTime UnixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public DocumentServiceLeaseCore()
        {
        }

        public DocumentServiceLeaseCore(DocumentServiceLeaseCore other)
        {
            this.LeaseId = other.LeaseId;
            this.DistributionUnit = other.DistributionUnit;
            this.Owner = other.Owner;
            this.ContinuationToken = other.ContinuationToken;
            this.ETag = other.ETag;
            this.TS = other.TS;
            this.ExplicitTimestamp = other.ExplicitTimestamp;
            this.Properties = other.Properties;
        }

        [JsonProperty("id")]
        public string LeaseId { get; set; }

        public override string Id => this.LeaseId;

        [JsonProperty("_etag")]
        public string ETag { get; set; }

        [JsonProperty("PartitionId")]
        public string DistributionUnit { get; set; }

        public override string ProcessingDistributionUnit => this.DistributionUnit;

        [JsonProperty("Owner")]
        public override string Owner { get; set; }

        /// <summary>
        /// Gets or sets the current value for the offset in the stream.
        /// </summary>
        [JsonProperty("ContinuationToken")]
        public override string ContinuationToken { get; set; }

        [JsonIgnore]
        public override DateTime Timestamp
        {
            get { return this.ExplicitTimestamp ?? UnixStartTime.AddSeconds(this.TS); }
            set { this.ExplicitTimestamp = value; }
        }

        [JsonIgnore]
        public override string ConcurrencyToken => this.ETag;

        [JsonProperty("properties")]
        public override Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        [JsonProperty("timestamp")]
        private DateTime? ExplicitTimestamp { get; set; }

        [JsonProperty("_ts")]
        private long TS { get; set; }

        public static DocumentServiceLeaseCore FromDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            string json = JsonConvert.SerializeObject(document);
            return JsonConvert.DeserializeObject<DocumentServiceLeaseCore>(json);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} Owner='{1}' Continuation={2} Timestamp(local)={3} Timestamp(server)={4}",
                this.Id,
                this.Owner,
                this.ContinuationToken,
                this.Timestamp.ToUniversalTime(),
                UnixStartTime.AddSeconds(this.TS).ToUniversalTime());
        }
    }
}