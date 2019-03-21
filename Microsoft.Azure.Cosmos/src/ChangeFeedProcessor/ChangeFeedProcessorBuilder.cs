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
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Bootstrapping;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Monitoring;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Utils;

    /// <summary>
    /// Provides a flexible way to to create an instance of <see cref="IChangeFeedProcessor"/> with custom set of parameters.
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
        private IChangeFeedObserverFactory observerFactory = null;
        private string databaseResourceId;
        private string collectionResourceId;
        private IParitionLoadBalancingStrategy loadBalancingStrategy;
        private IPartitionProcessorFactory partitionProcessorFactory = null;
        private IHealthMonitor healthMonitor;
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
        internal ILeaseStoreManager LeaseStoreManager
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
            this.observerFactory = new ChangeFeedObserverFactory<ChangeFeedObserverBase>(() => new ChangeFeedObserverBase(onChanges));
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

        ///// <summary>
        ///// Sets the <see cref="DocumentCollectionInfo"/> of the collection to listen for changes.
        ///// </summary>
        ///// <param name="feedCollectionLocation"><see cref="DocumentCollectionInfo"/> of the collection to listen for changes</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithFeedCollection(DocumentCollectionInfo feedCollectionLocation)
        //{
        //    if (feedCollectionLocation == null) throw new ArgumentNullException(nameof(feedCollectionLocation));
        //    this.feedCollectionLocation = feedCollectionLocation.Canonicalize();
        //    return this;
        //}

        ///// <summary>
        ///// Sets an existing <see cref="CosmosClient"/> to be used to read from the monitored collection.
        ///// </summary>
        ///// <param name="feedDocumentClient">The instance of <see cref="CosmosClient"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithFeedDocumentClient(CosmosClient feedDocumentClient)
        //{
        //    if (feedDocumentClient == null) throw new ArgumentNullException(nameof(feedDocumentClient));
        //    this.feedCosmosClient = new ChangeFeedDocumentClient(feedDocumentClient);
        //    return this;
        //}

        ///// <summary>
        ///// Sets an existing <see cref="IChangeFeedDocumentClient"/> to be used to read from the monitored collection.
        ///// </summary>
        ///// <param name="feedDocumentClient">The instance of <see cref="IChangeFeedDocumentClient"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithFeedDocumentClient(IChangeFeedDocumentClient feedDocumentClient)
        //{
        //    if (feedDocumentClient == null) throw new ArgumentNullException(nameof(feedDocumentClient));
        //    this.feedCosmosClient = feedDocumentClient;
        //    return this;
        //}

        /// <summary>
        /// Sets the <see cref="ChangeFeedProcessorOptions"/> to be used by this instance of <see cref="IChangeFeedProcessor"/>.
        /// </summary>
        /// <param name="changeFeedProcessorOptions">The instance of <see cref="ChangeFeedProcessorOptions"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithProcessorOptions(ChangeFeedProcessorOptions changeFeedProcessorOptions)
        {
            if (changeFeedProcessorOptions == null) throw new ArgumentNullException(nameof(changeFeedProcessorOptions));
            this.changeFeedProcessorOptions = changeFeedProcessorOptions;
            return this;
        }

        ///// <summary>
        ///// Sets the <see cref="FeedProcessing.IChangeFeedObserverFactory"/> to be used to generate <see cref="IChangeFeedObserver"/>
        ///// </summary>
        ///// <param name="observerFactory">The instance of <see cref="FeedProcessing.IChangeFeedObserverFactory"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithObserverFactory(FeedProcessing.IChangeFeedObserverFactory observerFactory)
        //{
        //    if (observerFactory == null) throw new ArgumentNullException(nameof(observerFactory));
        //    this.observerFactory = observerFactory;
        //    return this;
        //}

        ///// <summary>
        ///// Sets an existing <see cref="IChangeFeedObserver"/> type to be used by a <see cref="FeedProcessing.IChangeFeedObserverFactory"/> to process changes.
        ///// </summary>
        ///// <typeparam name="T">Type of the <see cref="IChangeFeedObserver"/>.</typeparam>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithObserver<T>()
        //    where T : FeedProcessing.IChangeFeedObserver, new()
        //{
        //    this.observerFactory = new ChangeFeedObserverFactory<T>();
        //    return this;
        //}

        ///// <summary>
        ///// Sets the Database Resource Id of the monitored collection.
        ///// </summary>
        ///// <param name="databaseResourceId">Database Resource Id.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithDatabaseResourceId(string databaseResourceId)
        //{
        //    if (databaseResourceId == null) throw new ArgumentNullException(nameof(databaseResourceId));
        //    this.databaseResourceId = databaseResourceId;
        //    return this;
        //}

        ///// <summary>
        ///// Sets the Collection Resource Id of the monitored collection.
        ///// </summary>
        ///// <param name="collectionResourceId">Collection Resource Id.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithCollectionResourceId(string collectionResourceId)
        //{
        //    if (collectionResourceId == null) throw new ArgumentNullException(nameof(collectionResourceId));
        //    this.collectionResourceId = collectionResourceId;
        //    return this;
        //}

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

        ///// <summary>
        ///// Sets an existing <see cref="DocumentClient"/> to be used to read from the leases collection.
        ///// </summary>
        ///// <param name="leaseDocumentClient">The instance of <see cref="DocumentClient"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithLeaseDocumentClient(DocumentClient leaseDocumentClient)
        //{
        //    if (leaseDocumentClient == null) throw new ArgumentNullException(nameof(leaseDocumentClient));
        //    this.leaseCosmosClient = new ChangeFeedDocumentClient(leaseDocumentClient);
        //    return this;
        //}

        ///// <summary>
        ///// Sets an existing <see cref="IChangeFeedDocumentClient"/> to be used to read from the leases collection.
        ///// </summary>
        ///// <param name="leaseDocumentClient">The instance of <see cref="IChangeFeedDocumentClient"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithLeaseDocumentClient(IChangeFeedDocumentClient leaseDocumentClient)
        //{
        //    if (leaseDocumentClient == null) throw new ArgumentNullException(nameof(leaseDocumentClient));
        //    this.leaseCosmosClient = leaseDocumentClient;
        //    return this;
        //}

        /// <summary>
        /// Sets the <see cref="IParitionLoadBalancingStrategy"/> to be used for partition load balancing
        /// </summary>
        /// <param name="strategy">The instance of <see cref="IParitionLoadBalancingStrategy"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithPartitionLoadBalancingStrategy(IParitionLoadBalancingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            this.loadBalancingStrategy = strategy;
            return this;
        }

        ///// <summary>
        ///// Sets the <see cref="IPartitionProcessorFactory"/> to be used to create <see cref="IPartitionProcessor"/> for partition processing.
        ///// </summary>
        ///// <param name="partitionProcessorFactory">The instance of <see cref="IPartitionProcessorFactory"/> to use.</param>
        ///// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        //public ChangeFeedProcessorBuilder WithPartitionProcessorFactory(IPartitionProcessorFactory partitionProcessorFactory)
        //{
        //    if (partitionProcessorFactory == null) throw new ArgumentNullException(nameof(partitionProcessorFactory));
        //    this.partitionProcessorFactory = partitionProcessorFactory;
        //    return this;
        //}

        /// <summary>
        /// Sets the <see cref="ILeaseStoreManager"/> to be used to manage leases.
        /// </summary>
        /// <param name="leaseStoreManager">The instance of <see cref="ILeaseStoreManager"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithLeaseStoreManager(ILeaseStoreManager leaseStoreManager)
        {
            if (leaseStoreManager == null) throw new ArgumentNullException(nameof(leaseStoreManager));
            this.LeaseStoreManager = leaseStoreManager;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="IHealthMonitor"/> to be used to monitor unhealthiness situation.
        /// </summary>
        /// <param name="healthMonitor">The instance of <see cref="IHealthMonitor"/> to use.</param>
        /// <returns>The instance of <see cref="ChangeFeedProcessorBuilder"/> to use.</returns>
        public ChangeFeedProcessorBuilder WithHealthMonitor(IHealthMonitor healthMonitor)
        {
            if (healthMonitor == null) throw new ArgumentNullException(nameof(healthMonitor));
            this.healthMonitor = healthMonitor;
            return this;
        }

        /// <summary>
        /// Builds a new instance of the <see cref="IChangeFeedProcessor"/> with the specified configuration.
        /// </summary>
        /// <returns>An instance of <see cref="IChangeFeedProcessor"/>.</returns>
        public async Task<IChangeFeedProcessor> BuildAsync()
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

            ILeaseStoreManager leaseStoreManager = await this.GetLeaseStoreManagerAsync(this.leaseContainer, true).ConfigureAwait(false);
            IPartitionManager partitionManager = this.BuildPartitionManager(leaseStoreManager);
            return new ChangeFeedProcessor(partitionManager);
        }

        /// <summary>
        /// Builds a new instance of the <see cref="IRemainingWorkEstimator"/> to estimate pending work with the specified configuration.
        /// </summary>
        /// <returns>An instance of <see cref="IRemainingWorkEstimator"/>.</returns>
        //public async Task<IRemainingWorkEstimator> BuildEstimatorAsync()
        //{
        //    if (this.feedCollectionLocation == null)
        //    {
        //        throw new InvalidOperationException(nameof(this.feedCollectionLocation) + " was not specified");
        //    }

        //    if (this.leaseCollectionLocation == null && this.LeaseStoreManager == null)
        //    {
        //        throw new InvalidOperationException($"Either {nameof(this.leaseCollectionLocation)} or {nameof(this.LeaseStoreManager)} must be specified");
        //    }

        //    await this.InitializeCollectionPropertiesForBuildAsync().ConfigureAwait(false);

        //    var leaseStoreManager = await this.GetLeaseStoreManagerAsync(this.leaseCollectionLocation, true).ConfigureAwait(false);

        //    IRemainingWorkEstimator remainingWorkEstimator = new RemainingWorkEstimator(
        //        leaseStoreManager,
        //        this.feedCosmosClient,
        //        this.feedCollectionLocation.GetCollectionSelfLink(),
        //        this.feedCollectionLocation.ConnectionPolicy.MaxConnectionLimit);
        //    return remainingWorkEstimator;
        //}

        //private static async Task<string> GetDatabaseResourceIdAsync(CosmosClient cosmosClient, DocumentCollectionInfo collectionLocation)
        //{
        //    Logger.InfoFormat("Reading database: '{0}'", collectionLocation.DatabaseName);
        //    Uri databaseUri = UriFactory.CreateDatabaseUri(collectionLocation.DatabaseName);
        //    var response = await documentClient.ReadDatabaseAsync(databaseUri, null).ConfigureAwait(false);
        //    return response.Resource.ResourceId;
        //}

        //private static async Task<string> GetCollectionResourceIdAsync(IChangeFeedDocumentClient documentClient, DocumentCollectionInfo collectionLocation)
        //{
        //    Logger.InfoFormat("Reading collection: '{0}'", collectionLocation.CollectionName);
        //    DocumentCollection documentCollection = await documentClient.GetDocumentCollectionAsync(collectionLocation).ConfigureAwait(false);
        //    return documentCollection.ResourceId;
        //}

        private IPartitionManager BuildPartitionManager(ILeaseStoreManager leaseStoreManager)
        {
            var factory = new CheckpointerObserverFactory(this.observerFactory, this.changeFeedProcessorOptions.CheckpointFrequency);
            var synchronizer = new PartitionSynchronizer(
                this.monitoredContainer,
                leaseStoreManager,
                leaseStoreManager,
                this.changeFeedProcessorOptions.DegreeOfParallelism,
                this.changeFeedProcessorOptions.QueryPartitionsMaxBatchSize);
            var bootstrapper = new Bootstrapper(synchronizer, leaseStoreManager, this.lockTime, this.sleepTime);
            var partitionSuperviserFactory = new PartitionSupervisorFactory(
                factory,
                leaseStoreManager,
                this.partitionProcessorFactory ?? new PartitionProcessorFactory(this.monitoredContainer, this.changeFeedProcessorOptions, leaseStoreManager),
                this.changeFeedProcessorOptions);

            if (this.loadBalancingStrategy == null)
            {
                this.loadBalancingStrategy = new EqualPartitionsBalancingStrategy(
                    this.HostName,
                    this.changeFeedProcessorOptions.MinPartitionCount,
                    this.changeFeedProcessorOptions.MaxPartitionCount,
                    this.changeFeedProcessorOptions.LeaseExpirationInterval);
            }

            IPartitionController partitionController = new PartitionController(leaseStoreManager, leaseStoreManager, partitionSuperviserFactory, synchronizer);

            if (this.healthMonitor == null)
            {
                this.healthMonitor = new TraceHealthMonitor();
            }

            partitionController = new HealthMonitoringPartitionControllerDecorator(partitionController, this.healthMonitor);
            var partitionLoadBalancer = new PartitionLoadBalancer(
                partitionController,
                leaseStoreManager,
                this.loadBalancingStrategy,
                this.changeFeedProcessorOptions.LeaseAcquireInterval);
            return new PartitionManager(bootstrapper, partitionController, partitionLoadBalancer);
        }

        private async Task<ILeaseStoreManager> GetLeaseStoreManagerAsync(
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
                    (IRequestOptionsFactory)new PartitionedByIdCollectionRequestOptionsFactory() :
                    (IRequestOptionsFactory)new SinglePartitionRequestOptionsFactory();

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