//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

using System;

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    internal class ChangeFeedObserverFactory<T> : IChangeFeedObserverFactory
        where T : IChangeFeedObserver, new()
    {
        private readonly Func<T> factoryInitializer;

        public ChangeFeedObserverFactory()
        {
            this.factoryInitializer = () => new T();
        }

        public ChangeFeedObserverFactory(Func<T> factoryInitializer)
        {
            this.factoryInitializer = factoryInitializer;
        }

        public IChangeFeedObserver CreateObserver()
        {
            return this.factoryInitializer();
        }
    }
}