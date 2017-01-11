// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Trestel.SqlQueryAnalyzer.Common
{
    /// <summary>
    /// Represent result of computation which may fail.
    /// </summary>
    /// <typeparam name="T">Type of resulting object of computation if successful.</typeparam>
    public struct Result<T> : IEquatable<Result<T>>
    {
        /// <summary>
        /// Represents empty result - failed without any errors.
        /// </summary>
        public static readonly Result<T> Empty = default(Result<T>);

        private readonly bool _isSuccess;
        private readonly T _successfulResult;
        private readonly ImmutableArray<string> _errors;

        private Result(bool success, T successfulResult, ImmutableArray<string> errors)
        {
            _isSuccess = success;
            _successfulResult = successfulResult;
            _errors = errors;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is success.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is success; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccess
        {
            get
            {
                return _isSuccess;
            }
        }

        /// <summary>
        /// Gets the successful result.
        /// </summary>
        /// <value>
        /// The successful result.
        /// </value>
        public T SuccessfulResult
        {
            get
            {
                if (!_isSuccess) throw new InvalidOperationException("Result does not represent success.");
                return _successfulResult;
            }
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        public ImmutableArray<string> Errors
        {
            get
            {
                if (_isSuccess) throw new InvalidOperationException("Result does not represent failure.");
                return _errors;
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
        public static bool operator ==(Result<T> obj1, Result<T> obj2)
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
        public static bool operator !=(Result<T> obj1, Result<T> obj2)
        {
            return !(obj1 == obj2);
        }

        /// <summary>
        /// Creates result that represent success.
        /// </summary>
        /// <param name="successfulResult">The successful result.</param>
        /// <returns>Result object with successful result encapsulated.</returns>
        public static Result<T> Success(T successfulResult)
        {
            return new Result<T>(true, successfulResult, ImmutableArray.Create<string>());
        }

        /// <summary>
        /// Creates result which represents failure with given error messages.
        /// </summary>
        /// <param name="errors">The errors.</param>
        /// <returns>Failed result.</returns>
        public static Result<T> Failure(ImmutableArray<string> errors)
        {
            return errors.Length > 0 ? new Result<T>(false, default(T), errors) : Empty;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Result<T> other)
        {
            return IsSuccess == other.IsSuccess &&
                ((IsSuccess && EqualityComparer<T>.Default.Equals(SuccessfulResult, other.SuccessfulResult)) ||
                (!IsSuccess && Errors.Equals(other.Errors)));
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
            return obj is Result<T> ? Equals((Result<T>)obj) : false;
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
                hash = hash * 31 + IsSuccess.GetHashCode();
                if (IsSuccess)
                {
                    hash = hash * 31 + (SuccessfulResult != null ? SuccessfulResult.GetHashCode() : 0);
                }
                else
                {
                    hash = hash * 31 + Errors.GetHashCode();
                }

                return hash;
            }
        }
    }

    /// <summary>
    /// Helper class containing construction functions for <see cref="Result{T}"/> object.
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Creates result that represent success.
        /// </summary>
        /// <typeparam name="T">Type of resulting object of computation if successful.</typeparam>
        /// <param name="successfulResult">The successful result.</param>
        /// <returns>
        /// Result object with successful result encapsulated.
        /// </returns>
        public static Result<T> Success<T>(T successfulResult)
        {
            return Result<T>.Success(successfulResult);
        }

        /// <summary>
        /// Creates result which represents failure with given error messages.
        /// </summary>
        /// <typeparam name="T">Type of resulting object of computation if successful.</typeparam>
        /// <param name="errors">The errors.</param>
        /// <returns>
        /// Failed result.
        /// </returns>
        public static Result<T> Failure<T>(IEnumerable<string> errors)
        {
            var arrayOfErrors = errors == null ? new string[0] : errors.ToArray();
            return Result<T>.Failure(ImmutableArray.Create(arrayOfErrors));
        }

        /// <summary>
        /// Creates result which represents failure with given error messages.
        /// </summary>
        /// <typeparam name="T">Type of resulting object of computation if successful.</typeparam>
        /// <param name="errors">The errors.</param>
        /// <returns>
        /// Failed result.
        /// </returns>
        public static Result<T> Failure<T>(params string[] errors)
        {
            return Result<T>.Failure(ImmutableArray.Create(errors));
        }
    }
}
