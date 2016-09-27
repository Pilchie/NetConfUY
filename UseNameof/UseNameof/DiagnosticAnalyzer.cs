using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UseNameof
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseNameofAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UseNameof";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string _category = "Correctness";

        private static DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, _title, _messageFormat, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(_rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction<SyntaxKind>(
                AnalyzeSyntax,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var argumentList = GetArguments(context.Node);
            if (argumentList.Arguments.Count == 0)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(
                context.Node,
                context.CancellationToken).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            // TODO: Handle named arguments.
            for (var i = 0; i < argumentList.Arguments.Count && i < methodSymbol.Parameters.Length; i++)
            {
                var argument = argumentList.Arguments[i].Expression;
                if (argument.Kind() == SyntaxKind.StringLiteralExpression &&
                    methodSymbol.Parameters[i].Name == "paramName")
                {
                    var parameters = context.Node
                        .FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()
                        ?.ParameterList.Parameters;

                    var argumentValue = ((LiteralExpressionSyntax)argument).Token.ValueText;
                    if (parameters?.Any(p => p.Identifier.ValueText == argumentValue) == true)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                _rule,
                                argument.GetLocation(),
                                $"{argumentValue}"));
                    }
                }
            }
        }

        private ArgumentListSyntax GetArguments(SyntaxNode node)
        {
            return node.Kind() == SyntaxKind.ObjectCreationExpression
                ? ((ObjectCreationExpressionSyntax)node).ArgumentList
                : ((InvocationExpressionSyntax)node).ArgumentList;
        }
    }
}
