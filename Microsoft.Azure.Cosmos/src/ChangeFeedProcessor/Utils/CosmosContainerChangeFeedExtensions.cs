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
        /// Initializes a <see cref="ChangeFeedProcessorBuilder{T}"/> for change feed processing.
        /// </summary>
        /// <param name="cosmosContainer">Cosmos Container that is being monitored for changes.</param>
        /// <param name="onChanges">Delegate to receive changes.</param>
        /// <returns></returns>
        public static ChangeFeedProcessorBuilder<T> CreateChangeFeedProcessorBuilder<T>(this CosmosContainer cosmosContainer, Func<IReadOnlyList<T>, CancellationToken, Task> onChanges)
        {
            return new ChangeFeedProcessorBuilder<T>(cosmosContainer, onChanges);
        }

        /// <summary>
        /// Initializes a <see cref="ChangeFeedProcessorBuilder{T}"/> for change feed estimating.
        /// </summary>
        /// <param name="cosmosContainer">Cosmos Container that is being monitored for changes.</param>
        /// <returns></returns>
        public static ChangeFeedProcessorBuilder<dynamic> CreateChangeFeedEstimatorBuilder(this CosmosContainer cosmosContainer)
        {
            return new ChangeFeedProcessorBuilder<dynamic>(cosmosContainer);
        }
    }
}
