//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Exceptions;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;

    internal sealed class LeaseRenewerCore : LeaseRenewer
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly DocumentServiceLeaseManager leaseManager;
        private readonly TimeSpan leaseRenewInterval;
        private DocumentServiceLease lease;

        public LeaseRenewerCore(DocumentServiceLease lease, DocumentServiceLeaseManager leaseManager, TimeSpan leaseRenewInterval)
        {
            this.lease = lease;
            this.leaseManager = leaseManager;
            this.leaseRenewInterval = leaseRenewInterval;
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.InfoFormat("Partition {0}: renewer task started.", this.lease.ProcessingDistributionUnit);
                await Task.Delay(TimeSpan.FromTicks(this.leaseRenewInterval.Ticks / 2), cancellationToken).ConfigureAwait(false);

                while (true)
                {
                    await this.RenewAsync().ConfigureAwait(false);
                    await Task.Delay(this.leaseRenewInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.InfoFormat("Partition {0}: renewer task stopped.", this.lease.ProcessingDistributionUnit);
            }
            catch (Exception ex)
            {
                Logger.FatalException("Partition {0}: renew lease loop failed", ex, this.lease.ProcessingDistributionUnit);
                throw;
            }
        }

        private async Task RenewAsync()
        {
            try
            {
                var renewedLease = await this.leaseManager.RenewAsync(this.lease).ConfigureAwait(false);
                if (renewedLease != null) this.lease = renewedLease;

                Logger.InfoFormat("Partition {0}: renewed lease with result {1}", this.lease.ProcessingDistributionUnit, renewedLease != null);
            }
            catch (LeaseLostException leaseLostException)
            {
                Logger.ErrorException("Partition {0}: lost lease on renew.", leaseLostException, this.lease.ProcessingDistributionUnit);
                throw;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Partition {0}: failed to renew lease.", ex, this.lease.ProcessingDistributionUnit);
            }
        }
    }
}