namespace Atc.Kusto.Analyzer.CodeFixes;

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

        // Find the class or record declaration identified by the diagnostic
        // The diagnostic span points to the class/record identifier, so we need to get the parent declaration
        var node = root.FindNode(diagnosticSpan);
        if (node.Parent is ClassDeclarationSyntax classDecl)
        {
            node = classDecl;
        }
        else if (node.Parent is RecordDeclarationSyntax recordDecl)
        {
            node = recordDecl;
        }

        if (node is not (ClassDeclarationSyntax or RecordDeclarationSyntax))
        {
            return;
        }

        // Get the syntax tree file path to calculate expected .kusto file name
        var classFilePath = await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        if (classFilePath is null || string.IsNullOrEmpty(classFilePath.FilePath))
        {
            return;
        }

        // Register a code fix to create the .kusto file
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Create empty .kusto file",
                createChangedSolution: c => CreateKustoFileAsync(context.Document, classFilePath.FilePath),
                equivalenceKey: nameof(MissingKustoScriptResourceCodeFixProvider)),
            context.Diagnostics);
    }

    private static Task<Solution> CreateKustoFileAsync(
        Document document,
        string classFilePath)
    {
        // Get the class file name and directory
        var fileName = classFilePath.GetFileNameWithoutExtension();
        var expectedKustoFileName = $"{fileName}.kusto";
        var directory = classFilePath.GetDirectoryName();

        // Normalize path separators for cross-platform compatibility (Roslyn workspace uses / internally)
        var normalizedDirectory = directory.Replace('\\', '/');
        var kustoFilePath = string.IsNullOrEmpty(normalizedDirectory)
            ? expectedKustoFileName
            : $"{normalizedDirectory}/{expectedKustoFileName}";

        // Create an empty .kusto file
        var kustoFileContent = string.Empty;

        // Add the .kusto file to the project
        var kustoDocument = document.Project.AddDocument(
            expectedKustoFileName,
            Microsoft.CodeAnalysis.Text.SourceText.From(kustoFileContent, System.Text.Encoding.UTF8),
            folders: null,
            filePath: kustoFilePath);

        return Task.FromResult(kustoDocument.Project.Solution);
    }
}