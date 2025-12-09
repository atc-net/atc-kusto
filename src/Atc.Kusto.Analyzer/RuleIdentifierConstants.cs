namespace Atc.Kusto.Analyzer;

internal static class RuleIdentifierConstants
{
    /// <summary>
    /// Design - RuleIdentifiers from ATCK001 to RuleIdentifier ATCK099.
    /// </summary>
    internal static class Design
    {
    }

    /// <summary>
    /// Naming - RuleIdentifiers from ATCK101 to RuleIdentifier ATCK199.
    /// </summary>
    internal static class Naming
    {
    }

    /// <summary>
    /// Style - RuleIdentifiers from ATCK201 to RuleIdentifier ATCK299.
    /// </summary>
    internal static class Style
    {
    }

    /// <summary>
    /// Usage - RuleIdentifiers from ATCK301 to RuleIdentifier ATCK399.
    /// </summary>
    internal static class Usage
    {
        internal const string MissingKustoScriptResource = "ATCK301";
        internal const string ParameterCountMismatch = "ATCK302";
        internal const string ParameterTypeMismatch = "ATCK303";
        internal const string ParameterOrderMismatch = "ATCK304";
        internal const string EmptyKustoScriptFile = "ATCK305";
        internal const string ProjectionFieldNotFound = "ATCK306";
        internal const string ResultPropertyNotProjected = "ATCK307";
        internal const string MissingFinalProjection = "ATCK308";
        internal const string ProjectionFieldNamingMismatch = "ATCK309";
    }

    /// <summary>
    /// Performance - RuleIdentifiers from ATCK401 to RuleIdentifier ATCK499.
    /// </summary>
    internal static class Performance
    {
    }

    /// <summary>
    /// Security - RuleIdentifiers from ATCK501 to RuleIdentifier ATCK599.
    /// </summary>
    internal static class Security
    {
    }
}