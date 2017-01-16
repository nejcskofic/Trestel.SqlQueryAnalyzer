// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis
{
    /// <summary>
    /// Struct containing parameter info.
    /// </summary>
    /// <seealso cref="System.IEquatable{T}" />
    public struct ParameterInfo : IEquatable<ParameterInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterInfo"/> struct.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public ParameterInfo(int position, string name, Type type)
        {
            Position = position;
            ParameterName = name;
            ParameterType = type;
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public int Position { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        /// <value>
        /// The name of the parameter.
        /// </value>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        /// <value>
        /// The type of the parameter.
        /// </value>
        public Type ParameterType { get; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ParameterInfo obj1, ParameterInfo obj2)
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
        public static bool operator !=(ParameterInfo obj1, ParameterInfo obj2)
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
        public bool Equals(ParameterInfo other)
        {
            return Position == other.Position &&
                ParameterName == other.ParameterName &&
                ParameterType == other.ParameterType;
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
            return obj is ParameterInfo ? this.Equals((ParameterInfo)obj) : false;
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
                hash = hash * 31 + Position;
                hash = hash * 31 + (ParameterName != null ? ParameterName.GetHashCode() : 0);
                hash = hash * 31 + (ParameterType != null ? ParameterType.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
