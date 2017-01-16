// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Helpers;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;
using Trestel.SqlQueryAnalyzer.Services;

namespace Trestel.SqlQueryAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer for checking raw SQL queries for syntactic correctness, parameter usage and result set mapping.
    /// </summary>
    /// <seealso cref="DiagnosticAnalyzer" />
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlQueryAnalyzer : DiagnosticAnalyzer
    {
        private readonly ServiceFactory _serviceFactory;
        private readonly CachingService _cachingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlQueryAnalyzer"/> class.
        /// </summary>
        public SqlQueryAnalyzer()
        {
            _serviceFactory = ServiceFactory.New().AddServices().Build();
            _cachingService = new CachingService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlQueryAnalyzer"/> class.
        /// Used for unit testing.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="System.ArgumentNullException">factory</exception>
        public SqlQueryAnalyzer(ServiceFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _serviceFactory = factory;
            _cachingService = new CachingService();
        }

        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this analyzer is capable of producing.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return SqlQueryAnalyzerDiagnostic.GetSupportedDiagnostics();
            }
        }

        /// <summary>
        /// Called once at session start to register actions in the analysis context.
        /// </summary>
        /// <param name="context">Context of analysis</param>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            AsyncHelper.RunSync(() => AnalyzeSymbolAsync(context));
        }

        // TODO: check behaviour if we have errors in code
        private async Task AnalyzeSymbolAsync(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;

            if (!IsSqlFromMethodCall(node, context.SemanticModel)) return;

            // load string
            if (node.ArgumentList.Arguments.Count != 1) return;
            var argumentExpression = node.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
            if (argumentExpression == null || argumentExpression.Kind() != SyntaxKind.StringLiteralExpression)
            {
                // raise unsupported diagnostics
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnsupportedDiagnostic(node.GetLocation()));
                return;
            }

            var rawSqlString = context.SemanticModel.GetConstantValue(argumentExpression).Value as string;
            if (String.IsNullOrEmpty(rawSqlString))
            {
                return;
            }

            var connectionData = node.RetrieveDatabaseConnectionHint(context.SemanticModel);
            if (!connectionData.IsDefined)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingDatabaseHintDiagnostic(node.ArgumentList.Arguments[0].GetLocation()));
                return;
            }

            // perform local analyis
            var methodNode = node.Parent.Parent.Parent as InvocationExpressionSyntax; // InvocationExpressionSyntax -> ArgumentSyntax -> ArgumentListSyntax -> InvocationExpressionSyntax
            Result<NormalizedCallSite> callSiteAnalysisResult = Result<NormalizedCallSite>.Empty;
            if (methodNode != null)
            {
                var callSiteContext = new CallSiteContext(methodNode, context.SemanticModel, context.CancellationToken);
                var callSiteAnalyzer = _serviceFactory.GetCallSiteAnalyzer(callSiteContext);
                if (callSiteAnalyzer != null)
                {
                    callSiteAnalysisResult = callSiteAnalyzer.AnalyzeCallSite(callSiteContext);
                    if (!callSiteAnalysisResult.IsSuccess)
                    {
                        // TODO: if failure, report failure diagnostic and abort
                        return;
                    }

                    if (!String.IsNullOrEmpty(callSiteAnalysisResult.SuccessfulResult.NormalizedSqlQuery))
                    {
                        rawSqlString = callSiteAnalysisResult.SuccessfulResult.NormalizedSqlQuery;
                    }
                }
            }

            Result<ValidatedQuery> result;
            try
            {
                result = await _cachingService.GetOrAddValidationResultAsync(
                    connectionData,
                    rawSqlString,
                    () => ValidateSqlStringAsync(rawSqlString, connectionData, argumentExpression, context.CancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // TODO: only squiggle SQL litteral
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateFailedToValidateDiagnostic(node.GetLocation(), ex.Message));
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            if (!result.IsSuccess)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateErrorsInSqlQueryDiagnostic(node.GetLocation(), result.Errors));
                return;
            }

            if (callSiteAnalysisResult.IsSuccess)
            {
                if (callSiteAnalysisResult.SuccessfulResult.CheckParameters)
                {
                    CheckParameterMapping(callSiteAnalysisResult.SuccessfulResult, result.SuccessfulResult, methodNode, context);
                }

                if (callSiteAnalysisResult.SuccessfulResult.CheckResult)
                {
                    CheckResultMapping(callSiteAnalysisResult.SuccessfulResult, result.SuccessfulResult, methodNode, context);
                }
            }
        }

        private static bool IsSqlFromMethodCall(InvocationExpressionSyntax node, SemanticModel semanticModel)
        {
            var nodeExpression = node.Expression as MemberAccessExpressionSyntax;
            if (nodeExpression == null) return false;

            if (nodeExpression.Name.Identifier.Text != "From") return false;

            SimpleNameSyntax classIdentifier = nodeExpression.Expression as IdentifierNameSyntax;
            if (classIdentifier == null)
            {
                var exprSyntx = nodeExpression.Expression as MemberAccessExpressionSyntax;
                classIdentifier = exprSyntx.Name;
            }

            // TODO: Type aliasing breaks this logic
            if (classIdentifier != null && classIdentifier.Identifier.Text != "Sql") return false;

            var nodeSymbol = semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (nodeSymbol == null) return false;

            if (nodeSymbol.ToDisplayString() != "Trestel.Database.Sql.From(string)") return false;

            return true;
        }

        private async Task<Result<ValidatedQuery>> ValidateSqlStringAsync(string rawSqlString, ConnectionStringData connectionData, LiteralExpressionSyntax target, CancellationToken cancellationToken)
        {
            // check if processing was canceled
            cancellationToken.ThrowIfCancellationRequested();

            var provider = _serviceFactory.GetQueryValidationProvider(connectionData);
            if (provider == null)
            {
                return Result.Failure<ValidatedQuery>($"There is no provider for database type {connectionData.DatabaseType.ToString()} and connection string {connectionData.DatabaseType}.");
            }

            if (provider.EnableThrottling && _cachingService.ContainsOrAddDocumentLocation(target))
            {
                await Task.Delay(750);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var result = await provider.ValidateAsync(rawSqlString, cancellationToken);
            return result;
        }

        private static void CheckParameterMapping(NormalizedCallSite callSite, ValidatedQuery validatedQuery, InvocationExpressionSyntax targetNode, SyntaxNodeAnalysisContext context)
        {
            var unusedParameters = new List<Parameter>(callSite.InputParameters);
            var missingParameters = new List<ParameterInfo>();

            for (int i = 0; i < validatedQuery.Parameters.Length; i++)
            {
                var queryParameter = validatedQuery.Parameters[i];
                bool found = false;
                for (int j = 0; j < callSite.InputParameters.Length; j++)
                {
                    var parameter = callSite.InputParameters[j];
                    if (queryParameter.ParameterName == parameter.ParameterName)
                    {
                        found = true;
                        unusedParameters.Remove(parameter);

                        var queryParameterType = queryParameter.ParameterType.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (queryParameterType != null && !parameter.ParameterType.CanAssign(queryParameterType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateParameterTypeMismatchDiagnostic(targetNode.GetLocation(), queryParameter.ParameterName, queryParameterType, parameter.ParameterType));
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
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedParameterDiagnostic(targetNode.GetLocation(), unusedParameters));
            }

            if (missingParameters.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingParameterDiagnostic(targetNode.GetLocation(), missingParameters));
            }
        }

        private static void CheckResultMapping(NormalizedCallSite callSite, ValidatedQuery validatedQuery, InvocationExpressionSyntax targetNode, SyntaxNodeAnalysisContext context)
        {
            // special case if we are directly mapping to primitive type
            if (callSite.ExpectedFields.Length == 1 && callSite.ExpectedFields[0].IsAnonymous)
            {
                var fieldType = callSite.ExpectedFields[0].FieldType;
                if (validatedQuery.OutputColumns.Length != 1)
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateExpectedSingleColumnInQueryResultDiagnostic(targetNode.GetLocation(), fieldType));
                    return;
                }

                var columnType = validatedQuery.OutputColumns[0].Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                if (columnType != null && !columnType.CanAssign(fieldType, context.SemanticModel.Compilation))
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateTypeMismatchDiagnostic(targetNode.GetLocation(), columnType, fieldType));
                }

                return;
            }

            var unusedColumns = new List<ColumnInfo>(validatedQuery.OutputColumns);
            var unusedFields = new List<ResultField>();

            for (int i = 0; i < callSite.ExpectedFields.Length; i++)
            {
                var field = callSite.ExpectedFields[i];
                bool found = false;
                for (int j = 0; j < validatedQuery.OutputColumns.Length; j++)
                {
                    var column = validatedQuery.OutputColumns[j];
                    if (field.FieldName == column.Name)
                    {
                        found = true;
                        unusedColumns.Remove(column);

                        var columnType = column.Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (columnType != null && !columnType.CanAssign(field.FieldType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreatePropertyTypeMismatchDiagnostic(targetNode.GetLocation(), column.Name, columnType, field.FieldType));
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
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingColumnsInQueryResultDiagnostic(targetNode.GetLocation(), unusedFields));
            }

            if (unusedColumns.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedColumnsInQueryResultDiagnostic(targetNode.GetLocation(), unusedColumns));
            }
        }
    }
}
