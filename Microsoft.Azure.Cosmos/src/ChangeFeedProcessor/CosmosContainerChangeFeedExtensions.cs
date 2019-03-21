//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions to 
    /// </summary>
    public static class CosmosContainerChangeFeedExtensions
    {
        /// <summary>
        /// Initializes a ChangeFeedBuilder
        /// </summary>
        /// <param name="cosmosContainer"></param>
        /// <param name="onChanges"></param>
        /// <returns></returns>
        public static ChangeFeedProcessorBuilder CreateChangeFeedProcessorBuilder (this CosmosContainer cosmosContainer, Func<IReadOnlyList<dynamic>, CancellationToken, Task> onChanges)
        {
            return new ChangeFeedProcessorBuilder(cosmosContainer, onChanges);
        }
    }
}
