namespace Atc.Kusto;

[Flags]
public enum FrameHeaders
{
    None = 0x00,
    DataSetHeader = 0x01,
    TableHeader = 0x02,
    CompletionSummary = 0x04,
    All = DataSetHeader | TableHeader | CompletionSummary,
}