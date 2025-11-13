namespace Atc.Kusto.Analyzer.Rules.Usage;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingKustoScriptResourceCodeFixProvider))]
[Shared]
public sealed class MissingKustoScriptResourceCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [RuleIdentifierConstants.Usage.MissingKustoScriptResource];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the class declaration identified by the diagnostic
        var node = root.FindNode(diagnosticSpan);
        if (node is not IdentifierNameSyntax && node.Parent is ClassDeclarationSyntax classDecl)
        {
            node = classDecl;
        }

        if (node is not ClassDeclarationSyntax)
        {
            return;
        }

        // Register a code fix to add a comment with instructions
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add instructions comment for creating .kusto file",
                createChangedDocument: c => AddInstructionsCommentAsync(context.Document, node, c),
                equivalenceKey: nameof(MissingKustoScriptResourceCodeFixProvider)),
            context.Diagnostics);
    }

    private static async Task<Document> AddInstructionsCommentAsync(
        Document document,
        SyntaxNode classDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || classDeclaration is not ClassDeclarationSyntax classDecl)
        {
            return document;
        }

        // Get the class file name
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree is null)
        {
            return document;
        }

        var fileName = GetFileNameWithoutExtension(syntaxTree.FilePath);
        var expectedKustoFileName = $"{fileName}.kusto";

        // Get line ending style
        var endOfLine = await GetEndOfLineTriviaFromDocumentAsync(document, root, cancellationToken).ConfigureAwait(false);

        // Create the instruction comment
        var commentText = $"// TODO: Create a file named '{expectedKustoFileName}' in the same directory as this file and mark it as an embedded resource in the .csproj";
        var commentTrivia = SyntaxFactory.Comment(commentText);

        // Get existing leading trivia
        var existingTrivia = classDecl.GetLeadingTrivia();

        // Add the comment before the class declaration
        var newTrivia = existingTrivia.Insert(0, commentTrivia);
        newTrivia = newTrivia.Insert(1, endOfLine);

        var newClassDecl = classDecl.WithLeadingTrivia(newTrivia);

        var newRoot = root.ReplaceNode(classDecl, newClassDecl);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string GetFileNameWithoutExtension(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        var fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
    }

    private static async Task<SyntaxTrivia> GetEndOfLineTriviaFromDocumentAsync(
        Document document,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        // Try to read the end_of_line setting from EditorConfig
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree is not null)
        {
            var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            if (options.TryGetValue("end_of_line", out var endOfLineValue))
            {
                return endOfLineValue switch
                {
                    "crlf" => SyntaxFactory.CarriageReturnLineFeed,
                    "lf" => SyntaxFactory.LineFeed,
                    "cr" => SyntaxFactory.CarriageReturn,
                    _ => SyntaxFactory.LineFeed,
                };
            }
        }

        // Fallback: check the source text for line endings
        var sourceText = await root.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        foreach (var line in sourceText.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
            {
                var lineBreakText = sourceText.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(line.End, line.EndIncludingLineBreak - line.End));
                return lineBreakText == "\r\n"
                    ? SyntaxFactory.CarriageReturnLineFeed
                    : SyntaxFactory.LineFeed;
            }
        }

        // Default to LF if we can't detect
        return SyntaxFactory.LineFeed;
    }
}
