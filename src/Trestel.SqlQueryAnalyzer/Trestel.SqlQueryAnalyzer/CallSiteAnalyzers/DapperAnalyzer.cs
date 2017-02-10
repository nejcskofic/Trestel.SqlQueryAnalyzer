// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.CallSiteAnalyzers
{
    /// <summary>
    /// Analyses call site which is using Dapper extension methods.
    /// </summary>
    /// <seealso cref="ICallSiteAnalyzer" />
    public class DapperAnalyzer : GenericAnalyzer
    {
        private const string _intrinsicParameterSuffix = "__sqlaintr";

        /// <summary>
        /// Determines whether this instance [can analyze call site] the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can analyze call site] the specified context; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanAnalyzeCallSite(CallSiteContext context)
        {
            if (context.CallSiteMethodSymbol == null) return false;

            return context.CallSiteMethodSymbol.ContainingNamespace.Name == "Dapper";
        }

        /// <summary>
        /// Analyzes the call site.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Result of call site normalization
        /// </returns>
        public override Result<NormalizedQueryDefinition> NormalizeQueryDefinition(CallSiteContext context)
        {
            var builder = NormalizedQueryDefinition.New();
            AnalyzeParameters(context.CallSiteNode, context.SourceSqlQuery, context.CallSiteMethodSymbol, context.SemanticModel, builder);
            return Result.Success(builder.Build(true));
        }

        /// <summary>
        /// Checks the parameter mapping.
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected override void CheckParameterMapping(NormalizedQueryDefinition callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            var unusedParameters = new List<Parameter>(callSite.InputParameters);
            var missingParameters = new List<ParameterInfo>();

            for (int i = 0; i < validatedQuery.Parameters.Length; i++)
            {
                var queryParameter = validatedQuery.Parameters[i];

                // do not process parameters which were added just so that we enforce query structure
                if (queryParameter.ParameterName.EndsWith(_intrinsicParameterSuffix)) continue;

                bool found = false;
                for (int j = 0; j < callSite.InputParameters.Length; j++)
                {
                    var parameter = callSite.InputParameters[j];
                    if (queryParameter.ParameterName == parameter.ParameterName)
                    {
                        found = true;
                        unusedParameters.Remove(parameter);

                        var queryParameterType = queryParameter.ParameterType.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        var parameterType = parameter.ParameterType;
                        if (!parameterType.IsBasicType())
                        {
                            parameterType = parameterType.TryGetUnderlyingTypeFromIEnumerableT() ?? parameterType;
                        }

                        if (queryParameterType != null && !parameterType.CanAssign(queryParameterType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateParameterTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), queryParameter.ParameterName, queryParameterType, parameterType));
                        }

                        break;
                    }
                }

                if (!found)
                {
                    missingParameters.Add(queryParameter);
                }
            }

            // report diagnostic
            if (unusedParameters.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedParameterDiagnostic(context.CallSiteNode.GetLocation(), unusedParameters));
            }

            if (missingParameters.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingParameterDiagnostic(context.CallSiteNode.GetLocation(), missingParameters, context.SemanticModel.Compilation));
            }
        }

        /// <summary>
        /// Extracts the expected type of the result from method signature.
        /// </summary>
        /// <param name="methodSymbol">The method symbol.</param>
        /// <returns>
        /// Type symbol if successful, otherwise null.
        /// </returns>
        protected override ITypeSymbol ExtractExpectedResultType(IMethodSymbol methodSymbol)
        {
            // since we know Dapper API simplify checks to only generic methods and extract type directly
            if (!methodSymbol.IsGenericMethod) return null;

            // TODO: support mutlimapping
            if (methodSymbol.TypeArguments.Length != 1) return null;

            var returnType = methodSymbol.TypeArguments[0];
            if (returnType.TypeKind == TypeKind.Error) return null;

            return returnType;
        }

        private static void AnalyzeParameters(InvocationExpressionSyntax methodSyntax, string sqlQuery, IMethodSymbol method, SemanticModel model, NormalizedQueryDefinition.Builder builder)
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

            string modifiedQuery = null;
            foreach (var prop in namedParamsType.GetPropertiesWithPublicGetter())
            {
                var type = prop.Type;
                if (type.Name == "DbString" && type.ContainingNamespace.Name == "Dapper")
                {
                    // handle DbString as normal string type
                    type = model.Compilation.GetSpecialType(SpecialType.System_String);
                }

                if (!type.IsBasicType() && type.TryGetUnderlyingTypeFromIEnumerableT() != null)
                {
                    // we have multiple parameters, expand value
                    modifiedQuery = Regex.Replace(
                        modifiedQuery ?? sqlQuery,
                        GetParameterRegex(prop.Name),
                        match =>
                        {
                            var variableName = match.Groups[1].Value;

                            // cannot reuse same variable since this may break query analysis
                            return $"({variableName}, {variableName}{_intrinsicParameterSuffix})";
                        },
                        RegexOptions.Multiline | RegexOptions.CultureInvariant);
                }

                builder.WithParameter(prop.Name, type);
            }

            if (modifiedQuery != null)
            {
                builder.WithNormalizedSqlQuery(modifiedQuery);
            }
        }

        private static string GetParameterRegex(string parameterName)
        {
            return @"([?@:]" + Regex.Escape(parameterName) + @")(?!\w)";
        }
    }
}
