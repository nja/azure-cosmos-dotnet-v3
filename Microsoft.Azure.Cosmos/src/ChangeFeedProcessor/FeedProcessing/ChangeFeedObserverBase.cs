//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Internal;

    internal class ChangeFeedObserverBase : IChangeFeedObserver
    {
        private readonly Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges;

        public ChangeFeedObserverBase() { }

        public ChangeFeedObserverBase(Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges)
        {
            this.onChanges = onChanges;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            return Task.CompletedTask;
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            return Task.CompletedTask;
        }

        public Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            return this.onChanges(docs, cancellationToken);
        }
    }
}
