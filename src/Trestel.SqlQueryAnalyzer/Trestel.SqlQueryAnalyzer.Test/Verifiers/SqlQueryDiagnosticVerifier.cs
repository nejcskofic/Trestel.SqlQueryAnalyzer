using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
