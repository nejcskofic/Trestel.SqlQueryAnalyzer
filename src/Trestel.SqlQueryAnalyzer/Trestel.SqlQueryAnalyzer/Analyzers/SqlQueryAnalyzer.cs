using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Providers.SqlServer;

namespace Trestel.SqlQueryAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlQueryAnalyzer : DiagnosticAnalyzer
    {
        // TODO: Add error type when target type does not contain any properties (bug in provider or bug in code)
        public const string CategoryName = "SQL";
        public const string FailedToValidateDiagnosticId = "SQL0001";
        public const string ErrorsInSqlQueryDiagnosticId = "SQL0002";
        public const string UnsupportedDiagnosticId = "SQL0003";
        public const string MissingColumnsInQueryResultDiagnosticId = "SQL0004";
        public const string UnusedColumnsInQueryResultDiagnosticId = "SQL0005";
        public const string MismatchBetweenPropertyTypesDiagnosticId = "SQL0006";
        public const string MismatchBetweenTypesDiagnosticId = "SQL0007";
        public const string MissingDatabaseHintAttributeDiagnosticId = "SQL0008";

        private static readonly DiagnosticDescriptor failedToValidateDescriptor = new DiagnosticDescriptor(FailedToValidateDiagnosticId, "Unable to complete validation", "Could not validate query because of following error: {0}", CategoryName, DiagnosticSeverity.Info, true, "Analyzer should be able to complete validation", null);
        private static readonly DiagnosticDescriptor errorsInSqlQueryDescriptor = new DiagnosticDescriptor(ErrorsInSqlQueryDiagnosticId, "Error in SQL statement", "There are following errors in SQL query:\n{0}", CategoryName, DiagnosticSeverity.Error, true, "SQL query should be syntactically correct and use existing database objects.", null);
        private static readonly DiagnosticDescriptor unsupportedDescriptor = new DiagnosticDescriptor(UnsupportedDiagnosticId, "Validation not supported", "Validation of SQL query string that is not literal is not supported.", CategoryName, DiagnosticSeverity.Warning, true, "SQL query should be entered as string literal.", null);
        private static readonly DiagnosticDescriptor missingInResultDescriptor = new DiagnosticDescriptor(MissingColumnsInQueryResultDiagnosticId, "Missing columns", "Following columns were expected in result set, but were not found:\n{0}", CategoryName, DiagnosticSeverity.Error, true, "SQL query should return all expected columns.", null);
        private static readonly DiagnosticDescriptor unusedColumnsDescriptor = new DiagnosticDescriptor(UnusedColumnsInQueryResultDiagnosticId, "Unused columns", "Following columns were found in result set, but are not being used:\n{0}", CategoryName, DiagnosticSeverity.Error, true, "SQL query should return only necessary columns.", null);
        private static readonly DiagnosticDescriptor propertyTypeMismatchDescriptor = new DiagnosticDescriptor(MismatchBetweenPropertyTypesDiagnosticId, "Types do not match", "For column '{0}' expected type '{1}', but found type '{2}'.", CategoryName, DiagnosticSeverity.Error, true, "Property type should match column type.", null);
        private static readonly DiagnosticDescriptor typeMismatchDescriptor = new DiagnosticDescriptor(MismatchBetweenTypesDiagnosticId, "Types do not match", "Expected type '{0}', but found type '{1}'.", CategoryName, DiagnosticSeverity.Error, true, "Property type should match column type.", null);
        private static readonly DiagnosticDescriptor missingDatabaseHintDescriptor = new DiagnosticDescriptor(MissingDatabaseHintAttributeDiagnosticId, "Missing database hint", "Analysis cannot continue because there is no 'Trestel.Database.Design.DatabaseHintAttribute' attribute applied to method, class or assembly.", CategoryName, DiagnosticSeverity.Info, true, "Attribute 'Trestel.Database.Design.DatabaseHintAttribute' is required to specify connection to database for analisys.", null);

        private readonly ServiceFactory _serviceFactory;

        public SqlQueryAnalyzer()
        {
            _serviceFactory = new ServiceFactory();
            _serviceFactory.BuildUp();
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
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    failedToValidateDescriptor,
                    errorsInSqlQueryDescriptor,
                    unsupportedDescriptor,
                    missingInResultDescriptor,
                    unusedColumnsDescriptor,
                    propertyTypeMismatchDescriptor,
                    typeMismatchDescriptor,
                    missingDatabaseHintDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;

            var nodeExpression = node.Expression as MemberAccessExpressionSyntax;
            if (nodeExpression == null) return;

            if (nodeExpression.Name.Identifier.Text != "From") return;

            SimpleNameSyntax classIdentifier = nodeExpression.Expression as IdentifierNameSyntax;
            if (classIdentifier == null)
            {
                var exprSyntx = nodeExpression.Expression as MemberAccessExpressionSyntax;
                classIdentifier = exprSyntx.Name;
            }

            // TODO: Type aliasing breaks this logic
            if (classIdentifier != null && classIdentifier.Identifier.Text != "Sql") return;

            var nodeSymbol = context.SemanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (nodeSymbol == null) return;

            if (nodeSymbol.ToDisplayString() != "Trestel.Database.Sql.From(string)") return;

            // load string
            if (node.ArgumentList.Arguments.Count != 1) return;
            var argumentExpression = node.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
            if (argumentExpression == null || argumentExpression.Kind() != SyntaxKind.StringLiteralExpression)
            {
                // TODO: flow analysis and computation engine when argument is not literal?
                // raise unsupported diagnostics
                context.ReportDiagnostic(Diagnostic.Create(unsupportedDescriptor, node.ArgumentList.Arguments[0].GetLocation()));
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
                context.ReportDiagnostic(Diagnostic.Create(missingDatabaseHintDescriptor, node.ArgumentList.Arguments[0].GetLocation()));
                return;
            }

            // TODO cache
            var provider = _serviceFactory.GetQueryValidationProvider(connectionData.ConnectionString, connectionData.DatabaseType);
            if (provider == null) return;

            try
            {
                var result = provider.Validate(rawSqlString);
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

                    AnalyzeMethodCall(methodNode, result.ValidatedQuery, context);
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(errorsInSqlQueryDescriptor, node.GetLocation(), String.Join("\n", result.Errors)));
                }

            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(failedToValidateDescriptor, node.GetLocation(), ex.Message));
            }
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
                    context.ReportDiagnostic(Diagnostic.Create(typeMismatchDescriptor, method.GetLocation(), namedSourceType.ToDisplayString(), queryResultType.ToDisplayString()));
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
                            context.ReportDiagnostic(Diagnostic.Create(propertyTypeMismatchDescriptor, method.GetLocation(), entry.Name, namedSourceType.ToDisplayString(), namedTargetType.ToDisplayString()));
                        }
                    }
                }

                // report diagnostic
                if (missingTargets.Count > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(missingInResultDescriptor, method.GetLocation(), String.Join("\n", missingTargets)));
                }
                if (unusedColumns.Count > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(unusedColumnsDescriptor, method.GetLocation(), String.Join("\n", unusedColumns)));
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
