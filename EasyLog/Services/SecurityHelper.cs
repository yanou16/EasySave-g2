using System.Security.Cryptography;
using System.Text;

namespace EasyLog.Services
{
    /// <summary>
    /// Provides cryptographic utilities used internally by EasyLog.
    /// All members are stateless and thread-safe.
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Computes the SHA-256 hash of the given string and returns it as a Base64 string.
        /// </summary>
        /// <param name="data">UTF-8 string to hash.</param>
        /// <returns>Base64-encoded SHA-256 digest.</returns>
        public static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Returns the canonical absolute path for the given path string,
        /// ensuring consistent UNC-style formatting across all log entries.
        /// </summary>
        /// <param name="path">Raw file path.</param>
        /// <returns>Normalised absolute path.</returns>
        public static string NormalizePath(string path) => Path.GetFullPath(path);

        /// <summary>
        /// Builds the chained SHA-256 signature for a single log entry.
        /// The signature covers all fields plus the previous entry's hash,
        /// forming a tamper-evident chain (similar to a blockchain).
        /// </summary>
        /// <param name="timestamp">Entry timestamp.</param>
        /// <param name="backupName">Backup job name.</param>
        /// <param name="sourcePath">Source file path.</param>
        /// <param name="targetPath">Destination file path.</param>
        /// <param name="fileSize">File size in bytes.</param>
        /// <param name="transferTimeMs">Transfer time in milliseconds.</param>
        /// <param name="encryptionTimeMs">Encryption time in milliseconds, or a negative CryptoSoft error code.</param>
        /// <param name="previousHash">Hash of the preceding log entry, or "GENESIS".</param>
        /// <returns>Base64-encoded SHA-256 signature for this entry.</returns>
        public static string BuildLogSignature(
            string timestamp,
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs,
            string previousHash)
        {
            string raw =
                $"{timestamp}|{backupName}|{sourcePath}|{targetPath}|{fileSize}|{transferTimeMs}|{previousHash}";

            return ComputeHash(raw);
        }

        public static string BuildLogSignature(
            string timestamp,
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs,
            long encryptionTimeMs,
            string previousHash)
        {
            string raw =
                $"{timestamp}|{backupName}|{sourcePath}|{targetPath}|{fileSize}|{transferTimeMs}|{encryptionTimeMs}|{previousHash}";

            return ComputeHash(raw);
        }
    }
}
