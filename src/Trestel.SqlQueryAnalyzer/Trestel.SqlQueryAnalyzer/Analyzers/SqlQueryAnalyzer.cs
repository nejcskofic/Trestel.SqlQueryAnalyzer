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
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;
using Trestel.SqlQueryAnalyzer.Services;

namespace Trestel.SqlQueryAnalyzer.Analyzers
{
    /// <summary>
    /// Analyzer for checking raw SQL queries for syntactic correctness, parameter usage and result set mapping.
    /// </summary>
    /// <seealso cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer" />
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
            _serviceFactory = new ServiceFactory().BuildUp();
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

            if (result.IsSuccess)
            {
                // TODO -> validate success
                // TODO refactor
                if (!(node.Parent is ArgumentSyntax))
                {
                    // if this is not directly used as argument syntax, any analysis is not supported
                    // TODO Flow analysis?
                    return;
                }

                var methodNode = node.Parent.Parent.Parent as InvocationExpressionSyntax;
                if (methodNode == null) return;

                AnalyzeMethodCall(methodNode, result.SuccessfulResult, context);
            }
            else
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateErrorsInSqlQueryDiagnostic(node.GetLocation(), result.Errors));
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

        private static void AnalyzeMethodCall(InvocationExpressionSyntax method, ValidatedQuery validatedQueryInfo, SyntaxNodeAnalysisContext context)
        {
            var methodSymbol = context.SemanticModel.GetSymbolInfo(method).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                // try if method is locally declared
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
            }

            if (methodSymbol == null) return;

            // get return type
            var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
            if (returnType == null || returnType.TypeKind == TypeKind.Error) return;

            ITypeSymbol queryResultType = null;

            // two options: 1. IEnumerable<T> or 2. T
            if (returnType.SpecialType == SpecialType.System_Collections_IEnumerable || returnType.AllInterfaces.Any(x => x.SpecialType == SpecialType.System_Collections_IEnumerable))
            {
                // TODO multimapping?
                if (!returnType.IsGenericType || returnType.TypeParameters.Length != 1 || returnType.TypeArguments.Length != 1) return;

                queryResultType = returnType.TypeArguments[0];
            }
            else
            {
                queryResultType = returnType;
            }

            if (queryResultType.TypeKind == TypeKind.Error) return;

            if (queryResultType.Name == "dynamic")
            {
                // TODO report dynamic
                return;
            }

            // TODO also report that on the one side we have basic type and on the other we have multiple columns
            if (validatedQueryInfo.OutputColumns.Count == 1 && queryResultType.IsBasicType())
            {
                var namedSourceType = validatedQueryInfo.OutputColumns[0].Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                if (namedSourceType != null && !namedSourceType.CanAssign(queryResultType, context.SemanticModel.Compilation))
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateTypeMismatchDiagnostic(method.GetLocation(), namedSourceType, queryResultType));
                }
            }
            else
            {
                var namedResultType = queryResultType as INamedTypeSymbol;
                if (namedResultType == null) return;

                // normal user defined type
                var missingTargets = new List<string>();
                var unusedColumns = new List<string>();
                foreach (var entry in validatedQueryInfo.OutputColumns.FullJoin(LoadResultTypeProperties(namedResultType), x => x.Name, x => x.Item1, (x, y) => new { Name = x?.Name ?? y?.Item1, SourceType = x?.Type, TargetType = y?.Item2 }))
                {
                    if (entry.SourceType == null)
                    {
                        missingTargets.Add(entry.Name + " (" + entry.TargetType.ToDisplayString() + ")");
                    }
                    else if (entry.TargetType == null)
                    {
                        unusedColumns.Add(entry.Name);
                    }
                    else
                    {
                        var namedTargetType = entry.TargetType as INamedTypeSymbol;
                        if (namedResultType == null)
                        {
                            // type is dynamic?
                            continue;
                        }

                        var namedSourceType = entry.SourceType.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (namedSourceType == null)
                        {
                            // convertion failed?
                            continue;
                        }

                        if (!namedSourceType.CanAssign(namedTargetType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreatePropertyTypeMismatchDiagnostic(method.GetLocation(), entry.Name, namedSourceType, namedTargetType));
                        }
                    }
                }

                // report diagnostic
                if (missingTargets.Count > 0)
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingColumnsInQueryResultDiagnostic(method.GetLocation(), missingTargets));
                }

                if (unusedColumns.Count > 0)
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedColumnsInQueryResultDiagnostic(method.GetLocation(), unusedColumns));
                }
            }
        }

        private static IEnumerable<Tuple<string, ITypeSymbol>> LoadResultTypeProperties(INamedTypeSymbol symbol)
        {
            foreach (IPropertySymbol prop in symbol.GetMembers().Where(x => x.Kind == SymbolKind.Property))
            {
                if (prop.IsStatic || prop.IsIndexer || prop.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (prop.SetMethod == null || prop.SetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                yield return Tuple.Create(prop.Name, prop.Type);
            }
        }
    }
}
