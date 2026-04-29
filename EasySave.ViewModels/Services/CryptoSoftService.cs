using System.Diagnostics;

namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Adapter around the external CryptoSoft executable required by the ProSoft brief.
    /// </summary>
    public class CryptoSoftService
    {
        private const int CryptoSoftFailure = -99;
        private const string DefaultKey = "EasySave";

        private readonly string _cryptoSoftPath;

        public CryptoSoftService(string appDataDirectory)
        {
            _cryptoSoftPath = ResolveCryptoSoftPath(appDataDirectory);
        }

        public long EncryptIfRequired(string filePath, string configuredExtensions)
        {
            if (!ShouldEncrypt(filePath, configuredExtensions))
                return 0;

            if (string.IsNullOrWhiteSpace(_cryptoSoftPath))
                return CryptoSoftFailure;

            try
            {
                // CryptoSoft is an external tool required by the brief, so the app calls it as a process.
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = _cryptoSoftPath,
                    Arguments = $"\"{filePath}\" \"{DefaultKey}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (process is null)
                    return CryptoSoftFailure;

                process.WaitForExit();
                return process.ExitCode;
            }
            catch
            {
                return CryptoSoftFailure;
            }
        }

        private static bool ShouldEncrypt(string filePath, string configuredExtensions)
        {
            if (string.IsNullOrWhiteSpace(configuredExtensions))
                return false;

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            return configuredExtensions
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeExtension)
                .Any(configured => extension.Equals(configured, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeExtension(string extension)
        {
            return extension.StartsWith(".",
                    StringComparison.Ordinal)
                ? extension
                : $".{extension}";
        }

        private static string ResolveCryptoSoftPath(string appDataDirectory)
        {
            string executableName = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";

            // Check packaged and development locations before falling back to PATH.
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, executableName),
                Path.Combine(AppContext.BaseDirectory, "CryptoSoft", executableName),
                Path.Combine(appDataDirectory, "CryptoSoft", executableName),
                Path.Combine(Directory.GetCurrentDirectory(), executableName),
                Path.Combine(Directory.GetCurrentDirectory(), "CryptoSoft", executableName)
            };

            return candidates.FirstOrDefault(File.Exists) ?? executableName;
        }
    }
}
