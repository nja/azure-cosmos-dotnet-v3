﻿
//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Exceptions;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;

    internal sealed class DocumentServiceLeaseCheckpointerCore : DocumentServiceLeaseCheckpointer
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly DocumentServiceLeaseUpdater leaseUpdater;
        private readonly DocumentServiceLeaseStoreManagerSettings settings;
        private readonly RequestOptionsFactory requestOptionsFactory;

        public DocumentServiceLeaseCheckpointerCore(
            DocumentServiceLeaseUpdater leaseUpdater,
            DocumentServiceLeaseStoreManagerSettings settings,
            RequestOptionsFactory requestOptionsFactory)
        {
            this.leaseUpdater = leaseUpdater;
            this.settings = settings;
            this.requestOptionsFactory = requestOptionsFactory;
        }

        public override async Task<DocumentServiceLease> CheckpointAsync(DocumentServiceLease lease, string continuationToken)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (string.IsNullOrEmpty(continuationToken))
                throw new ArgumentException("continuationToken must be a non-empty string", nameof(continuationToken));

            return await this.leaseUpdater.UpdateLeaseAsync(
                lease,
                lease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != lease.Owner)
                    {
                        Logger.InfoFormat("{0} lease token was taken over by owner '{1}'", lease.CurrentLeaseToken, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    serverLease.ContinuationToken = continuationToken;
                    return serverLease;
                }).ConfigureAwait(false);
        }

    }
}
