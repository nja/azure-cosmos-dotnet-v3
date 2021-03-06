﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Internal;

    /// <summary>
    /// The helper function relates to the async Task.
    /// </summary>
    internal static class TaskHelper
    {
        static public Task InlineIfPossible(Func<Task> function, IRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (SynchronizationContext.Current == null)
            {
                if (retryPolicy == null)
                {
                    // shortcut
                    return function();
                }
                else
                {
                    return BackoffRetryUtility<int>.ExecuteAsync(async () =>
                    {
                        await function();
                        return 0;
                    }, retryPolicy, cancellationToken);
                }
            }
            else
            {
                if (retryPolicy == null)
                {
                    // shortcut
                    return Task.Run(function);
                }
                else
                {
                    return Task.Run(() => BackoffRetryUtility<int>.ExecuteAsync(async () =>
                    {
                        await function();
                        return 0;
                    }, retryPolicy, cancellationToken));
                }
            }
        }

        static public Task<TResult> InlineIfPossible<TResult>(Func<Task<TResult>> function, IRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (SynchronizationContext.Current == null)
            {
                if (retryPolicy == null)
                {
                    // shortcut
                    return function();
                }
                else
                {
                    return BackoffRetryUtility<TResult>.ExecuteAsync(() =>
                    {
                        return function();
                    }, retryPolicy, cancellationToken);
                }
            }
            else
            {
                if (retryPolicy == null)
                {
                    // shortcut
                    return Task.Run(function);
                }
                else
                {
                    return Task.Run ( () => BackoffRetryUtility<TResult>.ExecuteAsync(() =>
                    {
                        return function();
                    }, retryPolicy, cancellationToken));
                }
            }
        }
    }
}
