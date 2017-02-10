// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Trestel.SqlQueryAnalyzer.Analyzers;
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
    public class GenericAnalyzer : ICallSiteAnalyzer
    {
        private static readonly Result<NormalizedQueryDefinition> _normalizeQueryDefinitionResult = Result.Success(NormalizedQueryDefinition.New().Build(false));

        /// <summary>
        /// Determines whether this instance can analyze call site given the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance can analyze call site; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanAnalyzeCallSite(CallSiteContext context)
        {
            return true;
        }

        /// <summary>
        /// Normalizes query definition.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Result of call site normalization
        /// </returns>
        public virtual Result<NormalizedQueryDefinition> NormalizeQueryDefinition(CallSiteContext context)
        {
            // Generic analyzer does not support parameter analysis since this is highly dependant of underlying library
            // Just return success with setting to check parameters turned off
            return _normalizeQueryDefinitionResult;
        }

        /// <summary>
        /// Verifies the call site.
        /// </summary>
        /// <param name="callSiteData">The call site data.</param>
        /// <param name="queryData">The query data.</param>
        /// <param name="context">The context.</param>
        public void AnalyzeCallSite(NormalizedQueryDefinition callSiteData, ValidatedQuery queryData, CallSiteVerificationContext context)
        {
            if (callSiteData.CheckParameters)
            {
                CheckParameterMapping(callSiteData, queryData, context);
            }

            // TODO
            // if (queryData.CheckResultMapping)
            CheckResultMapping(callSiteData, queryData, context);
        }

        /// <summary>
        /// Checks the parameter mapping.
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected virtual void CheckParameterMapping(NormalizedQueryDefinition callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            // NOOP: this analyzer does not know how to deal with parameters
        }

        /// <summary>
        /// Checks the result mapping.
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected virtual void CheckResultMapping(NormalizedQueryDefinition callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            var expectedType = ExtractExpectedResultType(context.CallSiteMethodSymbol);
            if (expectedType == null) return;

            // special case if we are directly mapping to primitive type
            if (expectedType.IsBasicType())
            {
                if (validatedQuery.OutputColumns.Length != 1)
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateExpectedSingleColumnInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), expectedType));
                    return;
                }

                var columnType = validatedQuery.OutputColumns[0].Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                if (columnType != null && !columnType.CanAssign(expectedType, context.SemanticModel.Compilation))
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), columnType, expectedType));
                }

                return;
            }

            // TODO if result is dynamic we might provide diagnostic with code fix for generating poco object
            var namedExpectedType = expectedType as INamedTypeSymbol;
            if (namedExpectedType == null) return;

            // mapping to POCO object
            var properties = namedExpectedType.GetPropertiesWithPublicSetter().ToList();
            var unusedColumns = new List<ColumnInfo>(validatedQuery.OutputColumns);
            var unusedFields = new List<IPropertySymbol>();

            for (int i = 0; i < properties.Count; i++)
            {
                var field = properties[i];
                bool found = false;
                for (int j = 0; j < validatedQuery.OutputColumns.Length; j++)
                {
                    var column = validatedQuery.OutputColumns[j];
                    if (field.Name == column.Name)
                    {
                        found = true;
                        unusedColumns.Remove(column);

                        var columnType = column.Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (columnType != null && !columnType.CanAssign(field.Type, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreatePropertyTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), column.Name, columnType, field.Type));
                        }

                        break;
                    }
                }

                if (!found)
                {
                    unusedFields.Add(field);
                }
            }

            // report diagnostic
            if (unusedFields.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingColumnsInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), unusedFields));
            }

            if (unusedColumns.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedColumnsInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), unusedColumns));
            }
        }

        /// <summary>
        /// Extracts the expected type of the result from method signature.
        /// </summary>
        /// <param name="methodSymbol">The method symbol.</param>
        /// <returns>Type symbol if successful, otherwise null.</returns>
        protected virtual ITypeSymbol ExtractExpectedResultType(IMethodSymbol methodSymbol)
        {
            // support only generic methods with single generic argument
            if (!methodSymbol.IsGenericMethod || methodSymbol.TypeParameters.Length != 1 || methodSymbol.TypeArguments.Length != 1) return null;

            var typeArgument = methodSymbol.TypeArguments[0];
            if (typeArgument.Kind == SymbolKind.ErrorType) return null;

            // generic method must have result IEnumerable<T> (or of compatible type) where T is type extracted above
            // TODO unwrap type if it is in Taks<T>
            var returnTypeArgument = methodSymbol.ReturnType.TryGetUnderlyingTypeFromIEnumerableT(true);
            if (returnTypeArgument != null && returnTypeArgument.Equals(typeArgument))
            {
                return typeArgument;
            }

            return null;
        }
    }
}
