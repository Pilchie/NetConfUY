using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;

namespace UseNameof
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofCodeFixProvider)), Shared]
    public class UseNameofCodeFixProvider : CodeFixProvider
    {
        private const string _title = "Replace with nameof";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(UseNameofAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var literal = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: _title,
                    createChangedDocument: c => ReplaceLiteralWithNameofAsync(context.Document, literal, c),
                    equivalenceKey: _title),
                diagnostic);
        }

        private async Task<Document> ReplaceLiteralWithNameofAsync(
            Document document,
            LiteralExpressionSyntax literal,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            editor.ReplaceNode(
                literal,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression("nameof")).AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.ParseExpression(literal.Token.ValueText))));
            return editor.GetChangedDocument();
        }
    }
}