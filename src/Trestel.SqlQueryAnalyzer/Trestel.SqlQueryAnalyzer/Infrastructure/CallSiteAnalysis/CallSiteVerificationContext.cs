// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Contains information necessary for performing verification between data from code and data from query.
    /// </summary>
    public class CallSiteVerificationContext
    {
        private readonly Action<Diagnostic> _reportAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallSiteVerificationContext" /> class.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="model">The model.</param>
        /// <param name="reportAction">The report action.</param>
        public CallSiteVerificationContext(InvocationExpressionSyntax node, SemanticModel model, Action<Diagnostic> reportAction)
        {
            CallSiteNode = node;
            SemanticModel = model;
            _reportAction = reportAction;
        }

        /// <summary>
        /// Gets the call site syntax node.
        /// </summary>
        /// <value>
        /// The call site node.
        /// </value>
        public InvocationExpressionSyntax CallSiteNode { get; }

        /// <summary>
        /// Gets the semantic model.
        /// </summary>
        /// <value>
        /// The semantic model.
        /// </value>
        public SemanticModel SemanticModel { get; }

        /// <summary>
        /// Reports the diagnostic.
        /// </summary>
        /// <param name="diagnostic">The diagnostic.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _reportAction(diagnostic);
        }
    }
}
