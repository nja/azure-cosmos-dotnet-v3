//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class ChangeFeedObserverBase : ChangeFeedObserver
    {
        private readonly Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges;

        public ChangeFeedObserverBase(Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges)
        {
            this.onChanges = onChanges;
        }

        public override Task CloseAsync(ChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            return Task.CompletedTask;
        }

        public override Task OpenAsync(ChangeFeedObserverContext context)
        {
            return Task.CompletedTask;
        }

        public override Task ProcessChangesAsync(ChangeFeedObserverContext context, IReadOnlyList<dynamic> docs, CancellationToken cancellationToken)
        {
            return this.onChanges(docs, cancellationToken);
        }
    }
}
