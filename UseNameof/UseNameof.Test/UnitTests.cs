using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using UseNameof;

namespace UseNameof.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void DoesNotShowInEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void ShowsForArgumentNullException()
        {
            var test = @"
class C 
{
    void M(string s)
    {
        if (s == null)
            throw new System.ArgumentNullException(""s"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "UseNameof",
                Message = "Use 'nameof(s)' instead of \"s\".",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 7, 52)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
class C 
{
    void M(string s)
    {
        if (s == null)
            throw new System.ArgumentNullException(nameof(s));
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UseNameofCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseNameofAnalyzer();
        }
    }
}