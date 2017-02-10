// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Contains information about call site of SQL query in C# code.
    /// </summary>
    public class CallSiteContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallSiteContext" /> class.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="symbol">The symbol.</param>
        /// <param name="sqlQuery">The SQL query.</param>
        /// <param name="semanticModel">The semantic model.</param>
        public CallSiteContext(InvocationExpressionSyntax node, IMethodSymbol symbol, string sqlQuery, SemanticModel semanticModel)
        {
            CallSiteNode = node;
            CallSiteMethodSymbol = symbol;
            SourceSqlQuery = sqlQuery;
            SemanticModel = semanticModel;
        }

        /// <summary>
        /// Gets the call site syntax node.
        /// </summary>
        /// <value>
        /// The call site node.
        /// </value>
        public InvocationExpressionSyntax CallSiteNode { get; }

        /// <summary>
        /// Gets the call site method symbol.
        /// </summary>
        /// <value>
        /// The call site method symbol.
        /// </value>
        public IMethodSymbol CallSiteMethodSymbol { get; }

        /// <summary>
        /// Gets the source SQL query.
        /// </summary>
        /// <value>
        /// The source SQL query.
        /// </value>
        public string SourceSqlQuery { get; }

        /// <summary>
        /// Gets the semantic model.
        /// </summary>
        /// <value>
        /// The semantic model.
        /// </value>
        public SemanticModel SemanticModel { get; }
    }
}
