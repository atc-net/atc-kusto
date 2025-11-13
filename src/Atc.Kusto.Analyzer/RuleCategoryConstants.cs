namespace Atc.Kusto.Analyzer;

internal static class RuleCategoryConstants
{
    /// <summary>
    /// Design - RuleIdentifiers from ATCK001 to RuleIdentifier ATCK099.
    /// </summary>
    public const string Design = nameof(Design);

    /// <summary>
    /// Naming - RuleIdentifiers from ATCK101 to RuleIdentifier ATCK199.
    /// </summary>
    public const string Naming = nameof(Naming);

    /// <summary>
    /// Style - RuleIdentifiers from ATCK201 to RuleIdentifier ATCK299.
    /// </summary>
    public const string Style = nameof(Style);

    /// <summary>
    /// Usage - RuleIdentifiers from ATCK301 to RuleIdentifier ATCK399.
    /// </summary>
    public const string Usage = nameof(Usage);

    /// <summary>
    /// Performance - RuleIdentifiers from ATCK401 to RuleIdentifier ATCK499.
    /// </summary>
    public const string Performance = nameof(Performance);

    /// <summary>
    /// Security - RuleIdentifiers from ATCK501 to RuleIdentifier ATCK599.
    /// </summary>
    public const string Security = nameof(Security);
}
