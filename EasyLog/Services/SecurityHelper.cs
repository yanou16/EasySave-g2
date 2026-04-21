using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace EasyLog.Services
{
    public static class SecurityHelper
    {
        public static string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path);
        }

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
    }
}
