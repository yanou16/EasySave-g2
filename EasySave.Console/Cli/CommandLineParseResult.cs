namespace EasySave.Console.Cli
{
    public sealed class CommandLineParseResult
    {
        public bool IsValid { get; init; }
        public bool IsInteractive { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<int> JobIndexes { get; init; } = Array.Empty<int>();
    }
}
