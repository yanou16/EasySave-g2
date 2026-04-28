namespace EasySave.Views.Console.Cli
{
    public class CommandLineParser
    {
        public CommandLineParseResult Parse(string[] args)
        {
            if (args.Length == 0)
                return new CommandLineParseResult { IsValid = true, IsInteractive = true };

            var indexes = new SortedSet<int>();

            foreach (string arg in args)
            {
                string[] tokens = arg.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if (token.Contains('-'))
                    {
                        if (!TryParseRange(token, indexes))
                            return Invalid($"Invalid range: {token}");
                        continue;
                    }

                    if (!int.TryParse(token, out int value) || value <= 0)
                        return Invalid($"Invalid backup selection: {token}");

                    indexes.Add(value - 1);
                }
            }

            return new CommandLineParseResult
            {
                IsValid       = true,
                IsInteractive = false,
                JobIndexes    = indexes.ToList()
            };
        }

        private static bool TryParseRange(string token, ISet<int> indexes)
        {
            string[] parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int end))
                return false;

            if (start <= 0 || end <= 0 || end < start) return false;

            for (int value = start; value <= end; value++)
                indexes.Add(value - 1);

            return true;
        }

        private static CommandLineParseResult Invalid(string message) =>
            new() { IsValid = false, IsInteractive = false, ErrorMessage = message };
    }
}
