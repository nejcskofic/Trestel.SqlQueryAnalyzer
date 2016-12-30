// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Infrastructure;

namespace TestHelper
{
    public abstract class SqlQueryDiagnosticVerifier : DiagnosticVerifier
    {
        protected void VerifyCSharpDiagnostic(ServiceFactory factory, string source, params DiagnosticResult[] expected)
        {
            VerifyDiagnostics(new[] { source }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(factory), expected);
        }

        protected void VerifyCSharpDiagnostic(ServiceFactory factory, string[] sources, params DiagnosticResult[] expected)
        {
            VerifyDiagnostics(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(factory), expected);
        }

        protected virtual DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer(ServiceFactory factory)
        {
            return new SqlQueryAnalyzer(factory);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            throw new InvalidOperationException("Should not call methods which do not supply service factory object to SqlQueryAnalyzer.");
        }
    }
}
