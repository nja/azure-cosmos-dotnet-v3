﻿//-----------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlLiteral.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------------------------------------------------------------------
using System;

namespace Microsoft.Azure.Cosmos.Sql
{
    internal abstract class SqlLiteral : SqlObject
    {
        protected SqlLiteral(SqlObjectKind kind)
            : base(kind)
        {
        }

        public abstract void Accept(SqlLiteralVisitor visitor);

        public abstract TResult Accept<TResult>(SqlLiteralVisitor<TResult> visitor);
    }
}
