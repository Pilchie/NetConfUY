using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.RegularExpressions;

namespace RegularExpressionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegularExpressionAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RegularExpressionAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Correctness";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var regexType = context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex");
            if (regexType != null)
            {
                var regexConstructors = regexType.GetMembers(WellKnownMemberNames.InstanceConstructorName);
                context.RegisterSyntaxNodeAction<SyntaxKind>(
                    c => AnalyzeSyntax(c, regexConstructors),
                    SyntaxKind.ObjectCreationExpression);
            }
        }

        private void AnalyzeSyntax(
            SyntaxNodeAnalysisContext context,
            ImmutableArray<ISymbol> regexConstructors)
        {
            var objectCreationExpression = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreationExpression.ArgumentList.Arguments.Count < 1)
            {
                return;
            }

            var patternArgument = objectCreationExpression.ArgumentList.Arguments.First().Expression;
            if (patternArgument.Kind() != SyntaxKind.StringLiteralExpression)
            {
                return;
            }

            var invokedMethodSymbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
            if (invokedMethodSymbol == null)
            {
                return;
            }

            if (regexConstructors.Contains(invokedMethodSymbol))
            {
                try
                {
                    new Regex(((LiteralExpressionSyntax)patternArgument).Token.ValueText);
                }
                catch (ArgumentException e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        patternArgument.GetLocation(),
                        e.Message));
                }
            }
        }
    }
}
