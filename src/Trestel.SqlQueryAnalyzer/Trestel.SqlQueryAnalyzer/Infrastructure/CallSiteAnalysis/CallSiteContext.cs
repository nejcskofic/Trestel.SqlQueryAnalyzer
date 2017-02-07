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
        /// <param name="sqlQuery">The SQL query.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public CallSiteContext(InvocationExpressionSyntax node, string sqlQuery, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            CallSiteNode = node;
            SourceSqlQuery = sqlQuery;
            SemanticModel = semanticModel;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets the call site syntax node.
        /// </summary>
        /// <value>
        /// The call site node.
        /// </value>
        public InvocationExpressionSyntax CallSiteNode { get; }

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

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        /// <value>
        /// The cancellation token.
        /// </value>
        public CancellationToken CancellationToken { get; }
    }
}
