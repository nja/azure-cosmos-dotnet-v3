//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    /// <summary>
    /// The ILeaseManager defines a way to perform operations with <see cref="ILease"/>.
    /// </summary>
    public interface ILeaseStoreManager : ILeaseContainer, ILeaseManager, ILeaseCheckpointer, ILeaseStore
    {
    }
}
