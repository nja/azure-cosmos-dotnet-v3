//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.Linq;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;

    internal sealed class RemainingWorkEstimatorCore : RemainingWorkEstimator
    {
        private const char PKRangeIdSeparator = ':';
        private const char SegmentSeparator = '#';
        private const string LSNPropertyName = "_lsn";
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CosmosContainer container;
        private readonly DocumentServiceLeaseContainer leaseContainer;
        private readonly int degreeOfParallelism;

        public RemainingWorkEstimatorCore(
            DocumentServiceLeaseContainer leaseContainer,
            CosmosContainer container,
            int degreeOfParallelism)
        {
            if (leaseContainer == null) throw new ArgumentNullException(nameof(leaseContainer));
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (degreeOfParallelism < 1) throw new ArgumentException("Degree of parallelism is out of range", nameof(degreeOfParallelism));

            this.leaseContainer = leaseContainer;
            this.container = container;
            this.degreeOfParallelism = degreeOfParallelism;
        }

        public override async Task<long> GetEstimatedRemainingWorkAsync()
        {
            var partitions = await this.GetEstimatedRemainingWorkPerPartitionAsync();
            if (partitions.Count == 0) return 1;

            return partitions.Sum(partition => partition.RemainingWork);
        }

        public override async Task<IReadOnlyList<RemainingPartitionWork>> GetEstimatedRemainingWorkPerPartitionAsync()
        {
            IReadOnlyList<DocumentServiceLease> leases = await this.leaseContainer.GetAllLeasesAsync().ConfigureAwait(false);
            if (leases == null || leases.Count == 0)
            {
                return new List<RemainingPartitionWork>().AsReadOnly();
            }

            var tasks = Partitioner.Create(leases)
                .GetPartitions(this.degreeOfParallelism)
                .Select(partition => Task.Run(async () =>
                {
                    var partialResults = new List<RemainingPartitionWork>();
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            DocumentServiceLease item = partition.Current;
                            try
                            {
                                if (string.IsNullOrEmpty(item?.ProcessingDistributionUnit)) continue;
                                var result = await this.GetRemainingWorkAsync(item);
                                partialResults.Add(new RemainingPartitionWork(item.ProcessingDistributionUnit, result));
                            }
                            catch (DocumentClientException ex)
                            {
                                Logger.WarnException($"Getting estimated work for {item.ProcessingDistributionUnit} failed!", ex);
                            }
                        }
                    }

                    return partialResults;
                })).ToArray();

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).ToList().AsReadOnly();
        }

        /// <summary>
        /// Parses a Session Token and extracts the LSN.
        /// </summary>
        /// <remarks>
        /// Session Token can be in two formats. Either {PartitionKeyRangeId}:{LSN} or {PartitionKeyRangeId}:{Version}#{GlobalLSN}.
        /// </remarks>
        /// <param name="sessionToken">A Session Token</param>
        /// <returns>Lsn value</returns>
        internal static string ExtractLsnFromSessionToken(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return string.Empty;
            }

            string parsedSessionToken = sessionToken.Substring(sessionToken.IndexOf(RemainingWorkEstimatorCore.PKRangeIdSeparator) + 1);
            string[] segments = parsedSessionToken.Split(RemainingWorkEstimatorCore.SegmentSeparator);

            if (segments.Length < 2)
            {
                return segments[0];
            }

            // GlobalLsn
            return segments[1];
        }

        private static Document GetFirstDocument(IFeedResponse<Document> response)
        {
            using (IEnumerator<Document> e = response.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    return e.Current;
                }
            }

            return null;
        }

        private static long TryConvertToNumber(string number)
        {
            long parsed = 0;
            if (!long.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                Logger.WarnFormat(string.Format(CultureInfo.InvariantCulture, "Cannot parse number '{0}'.", number));
                return 0;
            }

            return parsed;
        }

        private async Task<long> GetRemainingWorkAsync(DocumentServiceLease existingLease)
        {
            ChangeFeedOptions options = new ChangeFeedOptions
            {
                MaxItemCount = 1,
                PartitionKeyRangeId = existingLease.ProcessingDistributionUnit,
                RequestContinuation = existingLease.ContinuationToken,
                StartFromBeginning = string.IsNullOrEmpty(existingLease.ContinuationToken),
            };
            IDocumentQuery<Document> query = this.container.Client.DocumentClient.CreateDocumentChangeFeedQuery(container.LinkUri.ToString(), options);
            IFeedResponse<Document> response = null;

            try
            {
                response = await query.ExecuteNextAsync<Document>().ConfigureAwait(false);
                long parsedLSNFromSessionToken = TryConvertToNumber(ExtractLsnFromSessionToken(response.SessionToken));
                long lastQueryLSN = response.Count > 0
                    ? TryConvertToNumber(GetFirstDocument(response).GetPropertyValue<string>(LSNPropertyName)) - 1
                    : parsedLSNFromSessionToken;
                if (lastQueryLSN < 0)
                {
                    return 1;
                }

                long partitionRemainingWork = parsedLSNFromSessionToken - lastQueryLSN;
                return partitionRemainingWork < 0 ? 0 : partitionRemainingWork;
            }
            catch (Exception clientException)
            {
                Logger.WarnException($"GetEstimateWork > exception: partition '{existingLease.ProcessingDistributionUnit}'", clientException);
                throw;
            }
        }
    }
}
