//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement
{
    using System.Threading.Tasks;

    internal interface IPartitionLoadBalancer
    {
        /// <summary>
        /// Starts the load balancer
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the load balancer
        /// </summary>
        /// <returns>Task that completes once load balancer is fully stopped</returns>
        Task StopAsync();
    }
}