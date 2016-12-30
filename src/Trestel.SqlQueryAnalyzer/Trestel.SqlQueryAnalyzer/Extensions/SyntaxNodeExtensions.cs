// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Models;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="SyntaxNode"/>.
    /// </summary>
    internal static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Retrieves the database connection hint for given syntax node.
        /// </summary>
        /// <param name="syntaxNode">The syntax node.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <returns>Data about connection string if found.</returns>
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
}
