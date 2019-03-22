//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    internal sealed class ChangeFeedObserverFactoryCore: ChangeFeedObserverFactory
    {
        private readonly Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges;

        public ChangeFeedObserverFactoryCore(Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges)
        {
            this.onChanges = onChanges;
        }

        public override ChangeFeedObserver CreateObserver()
        {
            return new ChangeFeedObserverBase(this.onChanges);
        }
    }
}