// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Structure describing input parameter in parameterized query.
    /// </summary>
    /// <seealso cref="System.IEquatable{T}" />
    public struct Parameter : IEquatable<Parameter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> struct.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterType">Type of the parameter.</param>
        public Parameter(string parameterName, ITypeSymbol parameterType)
        {
            ParameterName = parameterName;
            ParameterType = parameterType;
        }

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
        public ITypeSymbol ParameterType { get; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Parameter obj1, Parameter obj2)
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
        public static bool operator !=(Parameter obj1, Parameter obj2)
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
        public bool Equals(Parameter other)
        {
            return ParameterName == other.ParameterName &&
                ((ParameterType == null && other.ParameterType == null) || (ParameterType != null && ParameterType.Equals(other.ParameterType)));
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
            return obj is Parameter ? this.Equals((Parameter)obj) : false;
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
                hash = hash * 31 + (ParameterName != null ? ParameterName.GetHashCode() : 0);
                hash = hash * 31 + (ParameterType != null ? ParameterType.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
