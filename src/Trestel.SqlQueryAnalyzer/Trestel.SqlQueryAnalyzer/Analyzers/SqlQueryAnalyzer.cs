﻿// Copyright (c) Nejc Skofic. All rights reserved.
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

            context.CancellationToken.ThrowIfCancellationRequested();

            // perform local analysis
            var methodNode = GetParentCallSiteSyntax(node);
            IMethodSymbol methodNodeSymbol = null;
            Result<NormalizedQueryDefinition> callSiteAnalysisResult = Result<NormalizedQueryDefinition>.Empty;
            ICallSiteAnalyzer callSiteAnalyzer = null;
            if (methodNode != null && (methodNodeSymbol = context.SemanticModel.GetSymbolInfo(methodNode).Symbol as IMethodSymbol) != null)
            {
                var callSiteContext = new CallSiteContext(methodNode, methodNodeSymbol, rawSqlString, context.SemanticModel);
                callSiteAnalyzer = _serviceFactory.GetCallSiteAnalyzer(callSiteContext);
                if (callSiteAnalyzer != null)
                {
                    callSiteAnalysisResult = callSiteAnalyzer.NormalizeQueryDefinition(callSiteContext);
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

            context.CancellationToken.ThrowIfCancellationRequested();

            Result<ValidatedQuery> result;
            try
            {
                result = await _cachingService.GetOrAddValidationResultAsync(
                    connectionData,
                    rawSqlString,
                    callSiteAnalysisResult.IsSuccess ? callSiteAnalysisResult.SuccessfulResult.CheckParameters : false,
                    (connection, sql, analyzeParameterInfo) => ValidateSqlStringAsync(connection, sql, analyzeParameterInfo, argumentExpression, context.CancellationToken));
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
                var verificationContext = new CallSiteVerificationContext(methodNode, methodNodeSymbol, context.SemanticModel, context.ReportDiagnostic);
                callSiteAnalyzer.AnalyzeCallSite(callSiteAnalysisResult.SuccessfulResult, result.SuccessfulResult, verificationContext);
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

        private async Task<Result<ValidatedQuery>> ValidateSqlStringAsync(ConnectionStringData connectionData, string rawSqlString, bool analyzeParameterInfo, LiteralExpressionSyntax target, CancellationToken cancellationToken)
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

            var result = await provider.ValidateAsync(rawSqlString, analyzeParameterInfo, cancellationToken);
            return result;
        }

        private static InvocationExpressionSyntax GetParentCallSiteSyntax(SyntaxNode node)
        {
            node = node.Parent;
            while (node != null && !(node is ArgumentSyntax))
            {
                if (!(node is CastExpressionSyntax)) return null;
                node = node.Parent;
            }

            // ArgumentSyntax -> ArgumentListSyntax -> InvocationExpressionSyntax
            return node != null ? node.Parent.Parent as InvocationExpressionSyntax : null;
        }
    }
}
