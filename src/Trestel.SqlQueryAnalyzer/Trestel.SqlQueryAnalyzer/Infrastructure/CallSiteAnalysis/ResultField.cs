// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Represents expected field in result set.
    /// </summary>
    /// <seealso cref="System.IEquatable{T}" />
    public struct ResultField : IEquatable<ResultField>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultField"/> struct.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <param name="containingSymbol">The containing symbol.</param>
        public ResultField(string fieldName, ITypeSymbol fieldType, ITypeSymbol containingSymbol)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            ContainingSymbol = containingSymbol;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        /// <value>
        /// The name of the field.
        /// </value>
        public string FieldName { get; }

        /// <summary>
        /// Gets the type of the field.
        /// </summary>
        /// <value>
        /// The type of the field.
        /// </value>
        public ITypeSymbol FieldType { get; }

        /// <summary>
        /// Gets the containing symbol.
        /// </summary>
        /// <value>
        /// The containing symbol.
        /// </value>
        public ITypeSymbol ContainingSymbol { get; }

        /// <summary>
        /// Gets a value indicating whether this instance represents anonymous result field.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is anonymous; otherwise, <c>false</c>.
        /// </value>
        public bool IsAnonymous
        {
            get
            {
                return String.IsNullOrEmpty(FieldName);
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ResultField obj1, ResultField obj2)
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
        public static bool operator !=(ResultField obj1, ResultField obj2)
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
        public bool Equals(ResultField other)
        {
            return FieldName == other.FieldName &&
                ((FieldType == null && other.FieldType == null) || (FieldType != null && FieldType.Equals(other.FieldType))) &&
                ((ContainingSymbol == null && other.ContainingSymbol == null) || (ContainingSymbol != null && ContainingSymbol.Equals(other.ContainingSymbol)));
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
                hash = hash * 31 + (FieldName != null ? FieldName.GetHashCode() : 0);
                hash = hash * 31 + (FieldType != null ? FieldType.GetHashCode() : 0);
                hash = hash * 31 + (ContainingSymbol != null ? ContainingSymbol.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
