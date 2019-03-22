﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.DocDBErrors
{
    using System.Net;
    using Microsoft.Azure.Cosmos.Internal;

    internal static class ExceptionClassifier
    {
        public static DocDbError ClassifyClientException(DocumentClientException clientException)
        {
            SubStatusCodes subStatusCode = clientException.GetSubStatusCode();

            if (clientException.StatusCode == HttpStatusCode.NotFound && subStatusCode != SubStatusCodes.ReadSessionNotAvailable)
                return DocDbError.PartitionNotFound;

            if (clientException.StatusCode == HttpStatusCode.Gone && (subStatusCode == SubStatusCodes.PartitionKeyRangeGone || subStatusCode == SubStatusCodes.CompletingSplit))
                return DocDbError.PartitionSplit;

            if (clientException.StatusCode == (HttpStatusCode)429 || clientException.StatusCode >= HttpStatusCode.InternalServerError)
                return DocDbError.TransientError;

            // Temporary workaround to compare exception message, until server provides better way of handling this case.
            if (clientException.Message.Contains("Reduce page size and try again."))
                return DocDbError.MaxItemCountTooLarge;

            return DocDbError.Undefined;
        }
    }
}