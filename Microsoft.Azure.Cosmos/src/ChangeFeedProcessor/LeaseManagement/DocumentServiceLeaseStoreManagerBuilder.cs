﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Utils;

    /// <summary>
    /// Provides flexible way to build lease manager constructor parameters.
    /// For the actual creation of lease manager instance, delegates to lease manager factory.
    /// </summary>
    internal class DocumentServiceLeaseStoreManagerBuilder
    {
        private DocumentServiceLeaseStoreManagerSettings settings = new DocumentServiceLeaseStoreManagerSettings();
        private CosmosContainer container;
        private IRequestOptionsFactory requestOptionsFactory;

        public DocumentServiceLeaseStoreManagerBuilder WithLeaseContainer(CosmosContainer leaseContainer)
        {
            if (leaseContainer == null) throw new ArgumentNullException(nameof(leaseContainer));

            this.container = leaseContainer;
            return this;
        }

        public DocumentServiceLeaseStoreManagerBuilder WithLeasePrefix(string leasePrefix)
        {
            if (leasePrefix == null) throw new ArgumentNullException(nameof(leasePrefix));

            this.settings.ContainerNamePrefix = leasePrefix;
            return this;
        }

        public DocumentServiceLeaseStoreManagerBuilder WithRequestOptionsFactory(IRequestOptionsFactory requestOptionsFactory)
        {
            if (requestOptionsFactory == null) throw new ArgumentNullException(nameof(requestOptionsFactory));

            this.requestOptionsFactory = requestOptionsFactory;
            return this;
        }

        public DocumentServiceLeaseStoreManagerBuilder WithHostName(string hostName)
        {
            if (hostName == null) throw new ArgumentNullException(nameof(hostName));

            this.settings.HostName = hostName;
            return this;
        }

        public Task<ILeaseStoreManager> BuildAsync()
        {
            if (this.container == null)
                throw new InvalidOperationException(nameof(this.container) + " was not specified");
            if (this.requestOptionsFactory == null)
                throw new InvalidOperationException(nameof(this.requestOptionsFactory) + " was not specified");

            var leaseStoreManager = new DocumentServiceLeaseStoreManager(this.settings, this.container, this.requestOptionsFactory);
            return Task.FromResult<ILeaseStoreManager>(leaseStoreManager);
        }
    }
}
