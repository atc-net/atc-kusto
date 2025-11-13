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
        // The diagnostic span points to the class identifier, so we need to get the parent ClassDeclarationSyntax
        var node = root.FindNode(diagnosticSpan);
        if (node.Parent is ClassDeclarationSyntax classDecl)
        {
            node = classDecl;
        }

        if (node is not ClassDeclarationSyntax classDeclNode)
        {
            return;
        }

        // Get the syntax tree file path to calculate expected .kusto file name
        var classFilePath = await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        if (classFilePath is null || string.IsNullOrEmpty(classFilePath.FilePath))
        {
            return;
        }

        var classFileName = GetFileNameWithoutExtension(classFilePath.FilePath);
        var expectedKustoFileName = $"{classFileName}.kusto";

        // Check if the TODO comment already exists
        var existingTrivia = classDeclNode.GetLeadingTrivia();
        if (existingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                                     t.ToString().Contains("TODO") &&
                                     t.ToString().Contains(expectedKustoFileName)))
        {
            // Comment already exists, don't offer the fix
            return;
        }

        // Register a code fix to create the .kusto file and add a TODO comment
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Create .kusto file with stub query",
                createChangedSolution: c => CreateKustoFileAndAddCommentAsync(context.Document, node, classFilePath.FilePath, c),
                equivalenceKey: nameof(MissingKustoScriptResourceCodeFixProvider)),
            context.Diagnostics);
    }

    private static async Task<Solution> CreateKustoFileAndAddCommentAsync(
        Document document,
        SyntaxNode classDeclaration,
        string classFilePath,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || classDeclaration is not ClassDeclarationSyntax classDecl)
        {
            return document.Project.Solution;
        }

        // Get the class file name and directory
        var fileName = GetFileNameWithoutExtension(classFilePath);
        var expectedKustoFileName = $"{fileName}.kusto";
        var directory = GetDirectoryPath(classFilePath);
        var kustoFilePath = string.IsNullOrEmpty(directory)
            ? expectedKustoFileName
            : $"{directory}\\{expectedKustoFileName}";

        // Get line ending style
        var endOfLine = await GetEndOfLineTriviaFromDocumentAsync(document, root, cancellationToken).ConfigureAwait(false);
        var endOfLineString = endOfLine.ToString();

        // Create stub .kusto file content with a placeholder query
        var kustoFileContent = $"// TODO: Replace this stub query with your actual Kusto query{endOfLineString}" +
                               $"// Example:{endOfLineString}" +
                               $"// TableName{endOfLineString}" +
                               $"// | where Condition{endOfLineString}" +
                               $"// | take 100{endOfLineString}" +
                               $"{endOfLineString}" +
                               $"TableName{endOfLineString}" +
                               $"| take 10{endOfLineString}";

        // Add the .kusto file to the project
        var kustoDocument = document.Project.AddDocument(
            expectedKustoFileName,
            Microsoft.CodeAnalysis.Text.SourceText.From(kustoFileContent, System.Text.Encoding.UTF8),
            folders: null,
            filePath: kustoFilePath);

        var updatedProject = kustoDocument.Project;

        // Now add a TODO comment to the C# class
        var commentText = $"// TODO: Fill out the '{expectedKustoFileName}' file with your actual query and add it to .csproj as <EmbeddedResource> and <AdditionalFiles>";

        // Get existing leading trivia
        var existingTrivia = classDecl.GetLeadingTrivia();

        // Find the last whitespace trivia (indentation) before the class keyword
        var indentationTrivia = existingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

        var commentTrivia = SyntaxFactory.Comment(commentText);

        // Create new trivia list: indentation + comment + newline + existing trivia
        var newTrivia = indentationTrivia.IsKind(SyntaxKind.None)
            ? SyntaxFactory.TriviaList(commentTrivia, endOfLine).AddRange(existingTrivia)
            : SyntaxFactory.TriviaList(indentationTrivia, commentTrivia, endOfLine).AddRange(existingTrivia);

        var newClassDecl = classDecl.WithLeadingTrivia(newTrivia);
        var newRoot = root.ReplaceNode(classDecl, newClassDecl);

        // Update the C# document in the project
        var updatedCSharpDocument = updatedProject.GetDocument(document.Id);
        if (updatedCSharpDocument is null)
        {
            return updatedProject.Solution;
        }

        updatedCSharpDocument = updatedCSharpDocument.WithSyntaxRoot(newRoot);

        return updatedCSharpDocument.Project.Solution;
    }

    private static string GetDirectoryPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        var lastSlash = Math.Max(filePath.LastIndexOf('/'), filePath.LastIndexOf('\\'));
        return lastSlash >= 0 ? filePath.Substring(0, lastSlash) : string.Empty;
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
