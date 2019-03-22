//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Bootstrapping;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Monitoring;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    /// <summary>
    /// Provides a flexible way to to create an instance of <see cref="ChangeFeedProcessor"/> with custom set of parameters.
    /// </summary>
    /// <example>
    /// <code language="C#">
    /// <![CDATA[
    /// // Observer.cs
    /// namespace Sample
    /// {
    ///     using System;
    ///     using System.Collections.Generic;
    ///     using System.Threading;
    ///     using System.Threading.Tasks;
    ///     using Microsoft.Azure.Documents;
    ///     using Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing;
    ///
    ///     class SampleObserver : IChangeFeedObserver
    ///     {
    ///         public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
    ///         {
    ///             return Task.CompletedTask;  // Note: requires targeting .Net 4.6+.
    ///         }
    ///
    ///         public Task OpenAsync(IChangeFeedObserverContext context)
    ///         {
    ///             return Task.CompletedTask;
    ///         }
    ///
    ///         public Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
    ///         {
    ///             Console.WriteLine("ProcessChangesAsync: partition {0}, {1} docs", context.PartitionKeyRangeId, docs.Count);
    ///             return Task.CompletedTask;
    ///         }
    ///     }
    /// }
    ///
    /// // Main.cs
    /// namespace Sample
    /// {
    ///     using System;
    ///     using System.Threading.Tasks;
    ///     using Microsoft.Azure.Cosmos.ChangeFeedProcessor;
    ///     using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    ///
    ///     class ChangeFeedProcessorSample
    ///     {
    ///         public static void Run()
    ///         {
    ///             RunAsync().Wait();
    ///         }
    ///
    ///         static async Task RunAsync()
    ///         {
    ///             DocumentCollectionInfo feedCollectionInfo = new DocumentCollectionInfo()
    ///             {
    ///                 DatabaseName = "DatabaseName",
    ///                 CollectionName = "MonitoredCollectionName",
    ///                 Uri = new Uri("https://sampleservice.documents.azure.com:443/"),
    ///                 MasterKey = "-- the auth key"
    ///             };
    ///
    ///             DocumentCollectionInfo leaseCollectionInfo = new DocumentCollectionInfo()
    ///             {
    ///                 DatabaseName = "DatabaseName",
    ///                 CollectionName = "leases",
    ///                 Uri = new Uri("https://sampleservice.documents.azure.com:443/"),
    ///                 MasterKey = "-- the auth key"
    ///             };
    ///
    ///             var builder = new ChangeFeedProcessorBuilder();
    ///             var processor = await builder
    ///                 .WithHostName("SampleHost")
    ///                 .WithFeedCollection(feedCollectionInfo)
    ///                 .WithLeaseCollection(leaseCollectionInfo)
    ///                 .WithObserver<SampleObserver>()
    ///                 .BuildAsync();
    ///
    ///             await processor.StartAsync();
    ///
    ///             Console.WriteLine("Change Feed Processor started. Press <Enter> key to stop...");
    ///             Console.ReadLine();
    ///
    ///             await processor.StopAsync();
    ///         }
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class ChangeFeedProcessorBuilder
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly long DefaultUnhealthinessDuration = TimeSpan.FromMinutes(15).Ticks;
        private readonly TimeSpan sleepTime = TimeSpan.FromSeconds(15);
        private readonly TimeSpan lockTime = TimeSpan.FromSeconds(30);
        private ChangeFeedProcessorOptions changeFeedProcessorOptions;
        private ChangeFeedObserverFactory observerFactory = null;
        private string databaseResourceId;
        private string collectionResourceId;
        private PartitionLoadBalancingStrategy loadBalancingStrategy;
        private PartitionProcessorFactory partitionProcessorFactory = null;
        private HealthMonitor healthMonitor;
        private CosmosContainer monitoredContainer;
        private CosmosContainer leaseContainer;

        internal string HostName
        {
            get; private set;
        }

        /// <summary>
        /// Gets the lease manager.
        /// </summary>
        /// <remarks>
        /// Internal for testing only, otherwise it would be private.
        /// </remarks>
        internal DocumentServiceLeaseStoreManager LeaseStoreManager
        {
            get;
            private set;
        }

        internal ChangeFeedProcessorBuilder(CosmosContainer cosmosContainer)
        {
            this.monitoredContainer = cosmosContainer;
        }

        internal ChangeFeedProcessorBuilder(CosmosContainer cosmosContainer, Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges)
            : this(cosmosContainer)
        {
            this.observerFactory = new ChangeFeedObserverFactoryCore(onChanges);
        }

        /// <summary>
        /// Sets the Host name.
        /// </summary>
        /// <param name="hostName">Name to be used for the host. When using multiple hosts, each host must have a unique name.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithHostName(string hostName)
        {
            this.HostName = hostName;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="ChangeFeedProcessorOptions"/> to be used by this instance of <see cref="ChangeFeedProcessor"/>.
        /// </summary>
        /// <param name="changeFeedProcessorOptions">The instance of <see cref="ChangeFeedProcessorOptions"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithProcessorOptions(ChangeFeedProcessorOptions changeFeedProcessorOptions)
        {
            if (changeFeedProcessorOptions == null) throw new ArgumentNullException(nameof(changeFeedProcessorOptions));
            this.changeFeedProcessorOptions = changeFeedProcessorOptions;
            return this;
        }

        /// <summary>
        /// Sets the Cosmos Con
        /// </summary>
        /// <param name="leaseContainer"></param>
        /// <returns></returns>
        public ChangeFeedProcessorBuilder WithLeaseContainer(CosmosContainer leaseContainer)
        {
            if (leaseContainer == null) throw new ArgumentNullException(nameof(leaseContainer));
            this.leaseContainer = leaseContainer;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="PartitionLoadBalancingStrategy"/> to be used for partition load balancing
        /// </summary>
        /// <param name="strategy">The instance of <see cref="PartitionLoadBalancingStrategy"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithPartitionLoadBalancingStrategy(PartitionLoadBalancingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            this.loadBalancingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="PartitionProcessorFactory"/> to be used to create <see cref="PartitionProcessor"/> for partition processing.
        /// </summary>
        /// <param name="partitionProcessorFactory">The instance of <see cref="PartitionProcessorFactory"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithPartitionProcessorFactory(PartitionProcessorFactory partitionProcessorFactory)
        {
            if (partitionProcessorFactory == null) throw new ArgumentNullException(nameof(partitionProcessorFactory));
            this.partitionProcessorFactory = partitionProcessorFactory;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="DocumentServiceLeaseStoreManager"/> to be used to manage leases.
        /// </summary>
        /// <param name="leaseStoreManager">The instance of <see cref="DocumentServiceLeaseStoreManager"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithLeaseStoreManager(DocumentServiceLeaseStoreManager leaseStoreManager)
        {
            if (leaseStoreManager == null) throw new ArgumentNullException(nameof(leaseStoreManager));
            this.LeaseStoreManager = leaseStoreManager;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="HealthMonitor"/> to be used to monitor unhealthiness situation.
        /// </summary>
        /// <param name="healthMonitor">The instance of <see cref="HealthMonitor"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithHealthMonitor(HealthMonitor healthMonitor)
        {
            if (healthMonitor == null) throw new ArgumentNullException(nameof(healthMonitor));
            this.healthMonitor = healthMonitor;
            return this;
        }

        /// <summary>
        /// Builds a new instance of the <see cref="ChangeFeedProcessor"/> with the specified configuration.
        /// </summary>
        /// <returns>An instance of <see cref="ChangeFeedProcessor"/>.</returns>
        public async Task<ChangeFeedProcessor> BuildAsync()
        {
            if (this.HostName == null)
            {
                throw new InvalidOperationException("Host name was not specified");
            }

            if (this.monitoredContainer == null)
            {
                throw new InvalidOperationException(nameof(this.monitoredContainer) + " was not specified");
            }

            if (this.leaseContainer == null && this.LeaseStoreManager == null)
            {
                throw new InvalidOperationException($"Either {nameof(this.leaseContainer)} or {nameof(this.LeaseStoreManager)} must be specified");
            }

            if (this.observerFactory == null)
            {
                throw new InvalidOperationException("Observer was not specified");
            }

            this.InitializeCollectionPropertiesForBuild();

            DocumentServiceLeaseStoreManager leaseStoreManager = await this.GetLeaseStoreManagerAsync(this.leaseContainer, true).ConfigureAwait(false);
            PartitionManager partitionManager = this.BuildPartitionManager(leaseStoreManager);
            return new ChangeFeedProcessorCore(partitionManager);
        }

        /// <summary>
        /// Builds a new instance of the <see cref="RemainingWorkEstimator"/> to estimate pending work with the specified configuration.
        /// </summary>
        /// <returns>An instance of <see cref="RemainingWorkEstimator"/>.</returns>
        public async Task<RemainingWorkEstimator> BuildEstimatorAsync()
        {
            if (this.monitoredContainer == null)
            {
                throw new InvalidOperationException(nameof(this.monitoredContainer) + " was not specified");
            }

            if (this.leaseContainer == null && this.LeaseStoreManager == null)
            {
                throw new InvalidOperationException($"Either {nameof(this.leaseContainer)} or {nameof(this.LeaseStoreManager)} must be specified");
            }

            this.InitializeCollectionPropertiesForBuild();

            var leaseStoreManager = await this.GetLeaseStoreManagerAsync(this.leaseContainer, true).ConfigureAwait(false);

            RemainingWorkEstimator remainingWorkEstimator = new RemainingWorkEstimatorCore(
                leaseStoreManager.LeaseContainer,
                this.monitoredContainer,
                this.monitoredContainer.Client.Configuration?.MaxConnectionLimit ?? 1);
            return remainingWorkEstimator;
        }

        private PartitionManager BuildPartitionManager(DocumentServiceLeaseStoreManager leaseStoreManager)
        {
            var factory = new CheckpointerObserverFactory(this.observerFactory, this.changeFeedProcessorOptions.CheckpointFrequency);
            var synchronizer = new PartitionSynchronizerCore(
                this.monitoredContainer,
                leaseStoreManager.LeaseContainer,
                leaseStoreManager.LeaseManager,
                this.changeFeedProcessorOptions.DegreeOfParallelism,
                this.changeFeedProcessorOptions.QueryPartitionsMaxBatchSize);
            var bootstrapper = new BootstrapperCore(synchronizer, leaseStoreManager.LeaseStore, this.lockTime, this.sleepTime);
            var partitionSuperviserFactory = new PartitionSupervisorFactoryCore(
                factory,
                leaseStoreManager.LeaseManager,
                this.partitionProcessorFactory ?? new PartitionProcessorFactoryCore(this.monitoredContainer, this.changeFeedProcessorOptions, leaseStoreManager.LeaseCheckpointer),
                this.changeFeedProcessorOptions);

            if (this.loadBalancingStrategy == null)
            {
                this.loadBalancingStrategy = new EqualPartitionsBalancingStrategy(
                    this.HostName,
                    this.changeFeedProcessorOptions.MinPartitionCount,
                    this.changeFeedProcessorOptions.MaxPartitionCount,
                    this.changeFeedProcessorOptions.LeaseExpirationInterval);
            }

            PartitionController partitionController = new PartitionControllerCore(leaseStoreManager.LeaseContainer, leaseStoreManager.LeaseManager, partitionSuperviserFactory, synchronizer);

            if (this.healthMonitor == null)
            {
                this.healthMonitor = new TraceHealthMonitor();
            }

            partitionController = new HealthMonitoringPartitionControllerDecorator(partitionController, this.healthMonitor);
            var partitionLoadBalancer = new PartitionLoadBalancerCore(
                partitionController,
                leaseStoreManager.LeaseContainer,
                this.loadBalancingStrategy,
                this.changeFeedProcessorOptions.LeaseAcquireInterval);
            return new PartitionManagerCore(bootstrapper, partitionController, partitionLoadBalancer);
        }

        private async Task<DocumentServiceLeaseStoreManager> GetLeaseStoreManagerAsync(
            CosmosContainer leaseContainer,
            bool isPartitionKeyByIdRequiredIfPartitioned)
        {
            if (this.LeaseStoreManager == null)
            {
                var cosmosContainerResponse = await this.leaseContainer.ReadAsync().ConfigureAwait(false);
                var containerSettings = cosmosContainerResponse.Resource;

                bool isPartitioned =
                    containerSettings.PartitionKey != null &&
                    containerSettings.PartitionKey.Paths != null &&
                    containerSettings.PartitionKey.Paths.Count > 0;
                if (isPartitioned && isPartitionKeyByIdRequiredIfPartitioned &&
                    (containerSettings.PartitionKey.Paths.Count != 1 || containerSettings.PartitionKey.Paths[0] != "/id"))
                {
                    throw new ArgumentException("The lease collection, if partitioned, must have partition key equal to id.");
                }

                var requestOptionsFactory = isPartitioned ?
                    (RequestOptionsFactory)new PartitionedByIdCollectionRequestOptionsFactory() :
                    (RequestOptionsFactory)new SinglePartitionRequestOptionsFactory();

                string leasePrefix = this.GetLeasePrefix();
                var leaseStoreManagerBuilder = new DocumentServiceLeaseStoreManagerBuilder()
                    .WithLeasePrefix(leasePrefix)
                    .WithLeaseContainer(this.leaseContainer)
                    .WithRequestOptionsFactory(requestOptionsFactory)
                    .WithHostName(this.HostName);

                this.LeaseStoreManager = await leaseStoreManagerBuilder.BuildAsync().ConfigureAwait(false);
            }

            return this.LeaseStoreManager;
        }

        private string GetLeasePrefix()
        {
            string optionsPrefix = this.changeFeedProcessorOptions.LeasePrefix ?? string.Empty;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}_{2}_{3}",
                optionsPrefix,
                this.monitoredContainer.Client.Configuration.AccountEndPoint.Host,
                this.databaseResourceId,
                this.collectionResourceId);
        }

        private void InitializeCollectionPropertiesForBuild()
        {
            this.changeFeedProcessorOptions = this.changeFeedProcessorOptions ?? new ChangeFeedProcessorOptions();
            var containerLinkSegments = this.monitoredContainer.LinkUri.OriginalString.Split('/');
            this.databaseResourceId = containerLinkSegments[2];
            this.collectionResourceId = containerLinkSegments[4];
        }
    }
}