//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    internal interface IDocumentServiceLeaseUpdater
    {
        Task<ILease> UpdateLeaseAsync(ILease cachedLease, string leaseId, object leasePartitionKey, Func<ILease, ILease> updateLease);
    }
}