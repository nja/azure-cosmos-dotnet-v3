﻿//-----------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlNumberLiteral.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Sql
{
    using System;
    using System.Globalization;
    using System.Text;
    using Collections.Generic;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class SqlNumberLiteral : SqlLiteral
    {
        private const int Capacity = 256;
        private static readonly Dictionary<long, SqlNumberLiteral> FrequentLongs = Enumerable
            .Range(-Capacity, Capacity)
            .ToDictionary(x => (long)x, x => new SqlNumberLiteral((long)x));
        private static readonly Dictionary<double, SqlNumberLiteral> FrequentDoubles = Enumerable
            .Range(-Capacity, Capacity)
            .ToDictionary(x => (double)x, x => new SqlNumberLiteral((double)x));

        private readonly Lazy<string> toStringValue;

        private SqlNumberLiteral(Number64 value)
            : base(SqlObjectKind.NumberLiteral)
        {
            this.Value = value;
            this.toStringValue = new Lazy<string>(() => 
            {
                return this.Value.ToString();
            });
        }

        public Number64 Value
        {
            get;
        }

        public override string ToString()
        {
            return this.toStringValue.Value;
        }

        public static SqlNumberLiteral Create(double number)
        {
            SqlNumberLiteral sqlNumberLiteral;
            if(!SqlNumberLiteral.FrequentDoubles.TryGetValue(number, out sqlNumberLiteral))
            {
                sqlNumberLiteral = new SqlNumberLiteral(number);
            }

            return sqlNumberLiteral;
        }

        public static SqlNumberLiteral Create(long number)
        {
            SqlNumberLiteral sqlNumberLiteral;
            if (!SqlNumberLiteral.FrequentLongs.TryGetValue(number, out sqlNumberLiteral))
            {
                sqlNumberLiteral = new SqlNumberLiteral(number);
            }

            return sqlNumberLiteral;
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

        public override void Accept(SqlLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override TResult Accept<TResult>(SqlLiteralVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
