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
        var fileName = GetFileNameWithoutExtension(classFilePath);
        var expectedKustoFileName = $"{fileName}.kusto";
        var directory = GetDirectoryPath(classFilePath);
        var kustoFilePath = string.IsNullOrEmpty(directory)
            ? expectedKustoFileName
            : $"{directory}\\{expectedKustoFileName}";

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
}