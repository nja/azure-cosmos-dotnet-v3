﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System;

    /// <summary>
    /// Factory class used to create instance(s) of <see cref="ChangeFeedObserver"/>.
    /// </summary>
    internal sealed class CheckpointerObserverFactory : ChangeFeedObserverFactory
    {
        private readonly ChangeFeedObserverFactory observerFactory;
        private readonly CheckpointFrequency checkpointFrequency;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointerObserverFactory"/> class.
        /// </summary>
        /// <param name="observerFactory">Instance of Observer Factory</param>
        /// <param name="checkpointFrequency">Defined <see cref="CheckpointFrequency"/></param>
        public CheckpointerObserverFactory(ChangeFeedObserverFactory observerFactory, CheckpointFrequency checkpointFrequency)
        {
            if (observerFactory == null)
                throw new ArgumentNullException(nameof(observerFactory));
            if (checkpointFrequency == null)
                throw new ArgumentNullException(nameof(checkpointFrequency));

            this.observerFactory = observerFactory;
            this.checkpointFrequency = checkpointFrequency;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ChangeFeedObserver"/>.
        /// </summary>
        /// <returns>Created instance of <see cref="ChangeFeedObserver"/>.</returns>
        public override ChangeFeedObserver CreateObserver()
        {
            ChangeFeedObserver observer = new ObserverExceptionWrappingChangeFeedObserverDecorator(this.observerFactory.CreateObserver());
            if (this.checkpointFrequency.ExplicitCheckpoint) return observer;

            return new AutoCheckpointer(this.checkpointFrequency, observer);
        }
    }
}