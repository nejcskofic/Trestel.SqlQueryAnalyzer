// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.CallSiteAnalyzers
{
    /// <summary>
    /// Default call site analyzer which performs conservative resulting object check and does not enable parameter checks.
    /// </summary>
    /// <seealso cref="Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis.ICallSiteAnalyzer" />
    public class GenericAnalyzer : BaseCallSiteAnalyzer
    {
        /// <summary>
        /// Determines whether this instance can analyze call site given the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance can analyze call site; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanAnalyzeCallSite(CallSiteContext context)
        {
            return true;
        }

        /// <summary>
        /// Analyzes the call site.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Result of call site normalization
        /// </returns>
        public override Result<NormalizedCallSite> AnalyzeCallSite(CallSiteContext context)
        {
            // TODO: error handling in syntax
            var methodSymbol = context.SemanticModel.GetSymbolInfo(context.CallSiteNode).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                // try if method is locally declared
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(context.CallSiteNode) as IMethodSymbol;
            }

            if (methodSymbol == null)
            {
                return Result.Success(NormalizedCallSite.New().Build(false, false));
            }

            // get return type
            var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
            if (returnType == null || returnType.TypeKind == TypeKind.Error)
            {
                return Result.Success(NormalizedCallSite.New().Build(false, false));
            }

            ITypeSymbol queryResultType = null;

            // two options: 1. IEnumerable<T> or 2. T
            if (returnType.SpecialType == SpecialType.System_Collections_IEnumerable || returnType.AllInterfaces.Any(x => x.SpecialType == SpecialType.System_Collections_IEnumerable))
            {
                if (!returnType.IsGenericType || returnType.TypeParameters.Length != 1 || returnType.TypeArguments.Length != 1)
                {
                    return Result.Success(NormalizedCallSite.New().Build(false, false));
                }

                queryResultType = returnType.TypeArguments[0];
            }
            else
            {
                queryResultType = returnType;
            }

            if (queryResultType.TypeKind == TypeKind.Error)
            {
                return Result.Success(NormalizedCallSite.New().Build(false, false));
            }

            if (queryResultType.Name == "dynamic")
            {
                // TODO report dynamic
                return Result.Success(NormalizedCallSite.New().Build(false, false));
            }

            if (queryResultType.IsBasicType())
            {
                var normalized = NormalizedCallSite.New()
                    .WithSingleAnonymousExpectedField(queryResultType)
                    .Build(false, true);
                return Result.Success(normalized);
            }
            else
            {
                var namedResultType = queryResultType as INamedTypeSymbol;
                if (namedResultType == null)
                {
                    return Result.Success(NormalizedCallSite.New().Build(false, false));
                }

                var builder = NormalizedCallSite.New();
                foreach (var prop in namedResultType.GetPropertiesWithPublicSetter())
                {
                    builder.WithExpectedField(prop.Name, prop.Type, namedResultType);
                }

                return Result.Success(builder.Build(false, true));
            }
        }

        /// <summary>
        /// Checks the parameter mapping.
        /// NOOP: this analyzer does not know how to deal with parameters
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected override void CheckParameterMapping(NormalizedCallSite callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            // NOOP: this analyzer does not know how to deal with parameters
        }
    }
}
