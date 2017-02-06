// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;

namespace Trestel.SqlQueryAnalyzer.CallSiteAnalyzers
{
    /// <summary>
    /// Analyses call site which is using Dapper extension methods.
    /// </summary>
    /// <seealso cref="ICallSiteAnalyzer" />
    public class DapperAnalyzer : ICallSiteAnalyzer
    {
        /// <summary>
        /// Determines whether this instance [can analyze call site] the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can analyze call site] the specified context; otherwise, <c>false</c>.
        /// </returns>
        public bool CanAnalyzeCallSite(CallSiteContext context)
        {
            if (context.CallSiteNode == null) return false;

            var nodeSymbol = context.SemanticModel.GetSymbolInfo(context.CallSiteNode).Symbol as IMethodSymbol;
            if (nodeSymbol == null) return false;

            return nodeSymbol.ContainingNamespace.Name == "Dapper";
        }

        /// <summary>
        /// Analyzes the call site.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Result of call site normalization
        /// </returns>
        public Result<NormalizedCallSite> AnalyzeCallSite(CallSiteContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var builder = NormalizedCallSite.New();
            var nodeSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(context.CallSiteNode).Symbol;

            AnalyzeParameters(context.CallSiteNode, nodeSymbol, context.SemanticModel, builder);
            bool shouldCheckReturnValue = AnalyzeReturnValue(nodeSymbol, builder);

            return Result.Success(builder.Build(true, shouldCheckReturnValue));
        }

        private static void AnalyzeParameters(InvocationExpressionSyntax methodSyntax, IMethodSymbol method, SemanticModel model, NormalizedCallSite.Builder builder)
        {
            ExpressionSyntax paramsExpressionSyntax = null;

            // try to load it by named argument
            var arguments = methodSyntax.ArgumentList.Arguments;
            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (argument.NameColon != null && argument.NameColon.Name.Identifier.ValueText == "param")
                {
                    paramsExpressionSyntax = argument.Expression;
                }
            }

            if (paramsExpressionSyntax == null)
            {
                // try load it by positional argument
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    var parameter = method.Parameters[i];
                    if (parameter.Name == "param" && i < arguments.Count)
                    {
                        paramsExpressionSyntax = arguments[i].Expression;
                        break;
                    }
                }
            }

            if (paramsExpressionSyntax == null) return;

            // TODO DynamicParameters for type
            var paramsType = model.GetTypeInfo(paramsExpressionSyntax).Type;
            if (paramsType == null || paramsType.TypeKind == TypeKind.Error) return;

            var underlyingType = paramsType.TryGetUnderlyingTypeFromIEnumerableT();
            if (underlyingType != null)
            {
                paramsType = underlyingType;
            }

            var namedParamsType = paramsType as INamedTypeSymbol;
            if (namedParamsType == null) return;

            foreach (var prop in namedParamsType.GetPropertiesWithPublicGetter())
            {
                var type = prop.Type;
                if (type.Name == "DbString" && type.ContainingNamespace.Name == "Dapper")
                {
                    // handle DbString as normal string type
                    type = model.Compilation.GetSpecialType(SpecialType.System_String);
                }

                builder.WithParameter(prop.Name, type);
            }
        }

        private static bool AnalyzeReturnValue(IMethodSymbol method, NormalizedCallSite.Builder builder)
        {
            // Currently non generic method are not supported.
            // We could only report return type as object/dynamic and raise diagnostic.
            if (!method.IsGenericMethod) return false;

            // TODO: support mutlimapping
            if (method.TypeArguments.Length != 1) return false;

            var returnType = method.TypeArguments[0];
            if (returnType.TypeKind == TypeKind.Error) return false;

            if (returnType.IsBasicType())
            {
                builder.WithSingleAnonymousExpectedField(returnType);
                return true;
            }
            else
            {
                var namedResultType = returnType as INamedTypeSymbol;
                if (namedResultType == null)
                {
                    return false;
                }

                foreach (var prop in namedResultType.GetPropertiesWithPublicSetter())
                {
                    builder.WithExpectedField(prop.Name, prop.Type, namedResultType);
                }

                return true;
            }
        }
    }
}
