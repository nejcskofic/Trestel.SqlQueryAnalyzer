using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Design;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    internal static class SyntaxNodeExtensions
    {
        public static ConnectionStringData RetrieveDatabaseConnectionHint(this SyntaxNode syntaxNode, SemanticModel semanticModel)
        {
            IAssemblySymbol assemblySymbol = null;

            // get parent method
            MethodDeclarationSyntax methodSyntax = null;
            while (syntaxNode != null)
            {
                methodSyntax = syntaxNode as MethodDeclarationSyntax;
                if (methodSyntax != null) break;
                syntaxNode = syntaxNode.Parent;
            }

            if (methodSyntax != null)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax) as IMethodSymbol;
                if (methodSymbol != null)
                {
                    assemblySymbol = methodSymbol.ContainingAssembly;
                    var data = ExtractDatabaseConnectionStringFromHintAttribute(methodSymbol.GetAttributes());
                    if (data.IsDefined) return data;
                }
            }

            // load class
            ClassDeclarationSyntax classSyntax = null;
            while (syntaxNode != null)
            {
                classSyntax = syntaxNode as ClassDeclarationSyntax;
                if (classSyntax != null) break;
                syntaxNode = syntaxNode.Parent;
            }

            if (classSyntax != null)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
                if (classSymbol != null)
                {
                    assemblySymbol = classSymbol.ContainingAssembly;
                    var data = ExtractDatabaseConnectionStringFromHintAttribute(classSymbol.GetAttributes());
                    if (data.IsDefined) return data;
                }
            }

            // load assembly
            if (assemblySymbol != null)
            {
                var data = ExtractDatabaseConnectionStringFromHintAttribute(assemblySymbol.GetAttributes());
                if (data.IsDefined) return data;
            }

            return ConnectionStringData.Empty;
        }

        private static ConnectionStringData ExtractDatabaseConnectionStringFromHintAttribute(ImmutableArray<AttributeData> attributes)
        {
            var databaseHintAttribute = attributes.FirstOrDefault(x => x.AttributeClass.ToDisplayString() == "Trestel.Database.Design.DatabaseHintAttribute");
            if (databaseHintAttribute == null) return ConnectionStringData.Empty;

            if (databaseHintAttribute.ConstructorArguments.Length == 1)
            {
                return new ConnectionStringData(databaseHintAttribute.ConstructorArguments[0].Value as string, DatabaseType.SqlServer);
            }
            else if (databaseHintAttribute.ConstructorArguments.Length == 2)
            {
                return new ConnectionStringData(databaseHintAttribute.ConstructorArguments[0].Value as string, (DatabaseType)databaseHintAttribute.ConstructorArguments[1].Value);
            }

            return ConnectionStringData.Empty;
        }
    }

    internal struct ConnectionStringData
    {
        public static readonly ConnectionStringData Empty = new ConnectionStringData();

        public string ConnectionString { get; }
        public DatabaseType DatabaseType { get; }
        public bool IsDefined { get { return ConnectionString != null; } }

        public ConnectionStringData(string connectionString, DatabaseType databaseType)
        {
            if (String.IsNullOrEmpty(connectionString)) throw new ArgumentException("Argument is null or empty.", nameof(connectionString));

            ConnectionString = connectionString;
            DatabaseType = databaseType;
        }
    }
}
