﻿// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis
{
    /// <summary>
    /// Immutable struct containing column information such as name, type and ordinal position.
    /// </summary>
    public struct ColumnInfo : IEquatable<ColumnInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnInfo" /> struct.
        /// </summary>
        /// <param name="ordinal">The ordinal.</param>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public ColumnInfo(int ordinal, string name, Type type)
        {
            Ordinal = ordinal;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the ordinal position.
        /// </summary>
        /// <value>
        /// The ordinal.
        /// </value>
        public int Ordinal
        {
            get;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public Type Type
        {
            get;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ColumnInfo obj1, ColumnInfo obj2)
        {
            return obj1.Equals(obj2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ColumnInfo obj1, ColumnInfo obj2)
        {
            return !(obj1 == obj2);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ColumnInfo other)
        {
            return Ordinal == other.Ordinal &&
                Name == other.Name &&
                Type == other.Type;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ColumnInfo ? this.Equals((ColumnInfo)obj) : false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Ordinal;
                hash = hash * 31 + (Name != null ? Name.GetHashCode() : 0);
                hash = hash * 31 + (Type != null ? Type.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
