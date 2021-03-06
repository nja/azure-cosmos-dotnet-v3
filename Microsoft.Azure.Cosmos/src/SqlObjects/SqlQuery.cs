﻿//-----------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlQuery.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Sql
{
    using System;

    internal class SqlQuery : SqlObject
    {
        protected SqlQuery(
            SqlSelectClause selectClause,
            SqlFromClause fromClause,
            SqlWhereClause whereClause,
            SqlOrderbyClause orderbyClause,
            SqlOffsetLimitClause offsetLimitClause)
            : base(SqlObjectKind.Query)
        {
            if (selectClause == null)
            {
                throw new ArgumentNullException($"{nameof(selectClause)} must not be null.");
            }

            this.SelectClause = selectClause;
            this.FromClause = fromClause;
            this.WhereClause = whereClause;
            this.OrderbyClause = orderbyClause;
            this.OffsetLimitClause = offsetLimitClause;
        }

        public SqlSelectClause SelectClause
        {
            get;
        }

        public SqlFromClause FromClause
        {
            get;
        }

        public SqlWhereClause WhereClause
        {
            get;
        }

        public SqlOrderbyClause OrderbyClause
        {
            get;
        }

        public SqlOffsetLimitClause OffsetLimitClause
        {
            get;
        }

        public static SqlQuery Create(
            SqlSelectClause selectClause,
            SqlFromClause fromClause,
            SqlWhereClause whereClause,
            SqlOrderbyClause orderbyClause,
            SqlOffsetLimitClause offsetLimitClause)
        {
            return new SqlQuery(selectClause, fromClause, whereClause, orderbyClause, offsetLimitClause);
        }

        public override void Accept(SqlObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }

        public override TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input)
        {
            return visitor.Visit(this, input);
        }
    }
}
