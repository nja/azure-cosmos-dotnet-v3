//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    internal class DocumentServiceLeaseStoreManagerSettings
    {
        internal string ContainerNamePrefix { get; set; }

        internal string HostName { get; set; }
    }
}
