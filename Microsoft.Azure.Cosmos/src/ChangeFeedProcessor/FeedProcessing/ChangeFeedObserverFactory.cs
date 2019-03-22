//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    /// <summary>
    /// Factory class used to create instance(s) of <see cref="ChangeFeedObserver"/>.
    /// </summary>

    public abstract class ChangeFeedObserverFactory
    {
        /// <summary>
        /// Creates an instance of a <see cref="ChangeFeedObserver"/>.
        /// </summary>
        /// <returns>An instance of a <see cref="ChangeFeedObserver"/>.</returns>
        public abstract ChangeFeedObserver CreateObserver();
    }
}