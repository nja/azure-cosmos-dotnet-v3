//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IPartitionSupervisor : IDisposable
    {
        Task RunAsync(CancellationToken shutdownToken);
    }
}