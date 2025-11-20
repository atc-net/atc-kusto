namespace Atc.Kusto.Analyzer.Helpers;

/// <summary>
/// Provides shared helper methods for Kusto analyzers.
/// </summary>
internal static class KustoAnalyzerHelper
{
    /// <summary>
    /// Determines whether the specified symbol should be analyzed by checking if it's a concrete class/struct
    /// that inherits from KustoScript.
    /// </summary>
    /// <param name="namedTypeSymbol">The named type symbol to check.</param>
    public static bool ShouldAnalyzeSymbol(INamedTypeSymbol namedTypeSymbol)
    {
        // Skip if this is an abstract class, interface, or not a class/record
        if (namedTypeSymbol.IsAbstract || (namedTypeSymbol.TypeKind != TypeKind.Class && namedTypeSymbol.TypeKind != TypeKind.Struct))
        {
            return false;
        }

        return InheritsFromKustoScript(namedTypeSymbol);
    }

    /// <summary>
    /// Gets the first syntax reference for the specified symbol, or null if none exists.
    /// </summary>
    /// <param name="namedTypeSymbol">The named type symbol to get the syntax reference from.</param>
    public static SyntaxReference? GetSyntaxReference(INamedTypeSymbol namedTypeSymbol)
    {
        var syntaxReferences = namedTypeSymbol.DeclaringSyntaxReferences;
        return syntaxReferences.Length > 0 ? syntaxReferences[0] : null;
    }

    /// <summary>
    /// Gets the identifier token for a class or record declaration.
    /// </summary>
    /// <param name="syntaxReference">The syntax reference to extract the identifier from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static SyntaxToken? GetTypeIdentifier(
        SyntaxReference syntaxReference,
        CancellationToken cancellationToken)
    {
        var declaration = syntaxReference.GetSyntax(cancellationToken);

        return declaration switch
        {
            ClassDeclarationSyntax classDecl => classDecl.Identifier,
            RecordDeclarationSyntax recordDecl => recordDecl.Identifier,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the location of the type identifier for diagnostic reporting.
    /// </summary>
    /// <param name="syntaxReference">The syntax reference to extract the location from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Location? GetDiagnosticLocation(
        SyntaxReference syntaxReference,
        CancellationToken cancellationToken)
        => GetTypeIdentifier(syntaxReference, cancellationToken)?.GetLocation();

    /// <summary>
    /// Finds the corresponding .kusto file for a given class file path.
    /// </summary>
    /// <param name="additionalFiles">The collection of additional files provided to the analyzer.</param>
    /// <param name="classFilePath">The file path of the C# class to find a matching .kusto file for.</param>
    public static AdditionalText? FindKustoFile(
        ImmutableArray<AdditionalText> additionalFiles,
        string classFilePath)
    {
        var classFileName = classFilePath.GetFileNameWithoutExtension();
        var classDirectory = classFilePath.GetDirectoryName();
        var expectedKustoFileName = $"{classFileName}.kusto";

        // Find the corresponding .kusto file in AdditionalFiles
        // Match by both filename and directory to handle multiple files with the same name
        return additionalFiles.FirstOrDefault(file =>
        {
            if (string.IsNullOrEmpty(file.Path))
            {
                return false;
            }

            var fileName = file.Path.GetFileName();
            if (!fileName.Equals(expectedKustoFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileDirectory = file.Path.GetDirectoryName();
            return fileDirectory.Equals(classDirectory, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Checks whether a .kusto file exists for the given class file path.
    /// </summary>
    /// <param name="additionalFiles">The collection of additional files provided to the analyzer.</param>
    /// <param name="classFilePath">The file path of the C# class to check for a matching .kusto file.</param>
    public static bool KustoFileExists(
        ImmutableArray<AdditionalText> additionalFiles,
        string classFilePath)
        => FindKustoFile(additionalFiles, classFilePath) is not null;

    /// <summary>
    /// Determines whether the specified type symbol inherits from KustoScript.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check for KustoScript inheritance.</param>
    public static bool InheritsFromKustoScript(INamedTypeSymbol typeSymbol)
    {
        var baseType = typeSymbol.BaseType;

        while (baseType != null)
        {
            if (baseType.Name == Constants.KustoScriptBaseClassName)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}