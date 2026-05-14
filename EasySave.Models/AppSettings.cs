namespace EasySave.Models
{
    /// <summary>
    /// Stores user-configurable application settings persisted in config.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Log file format. Accepted values: "JSON" or "XML".</summary>
        public string LogFormat { get; set; } = "JSON";

        /// <summary>Process name of the business software (e.g. "calc"). Empty = disabled.</summary>
        public string BusinessSoftware { get; set; } = string.Empty;

        /// <summary>Semicolon-separated list of extensions to encrypt (e.g. ".txt;.docx").</summary>
        public string CryptoExtensions { get; set; } = string.Empty;

        /// <summary>UI language. Accepted values: "en" or "fr".</summary>
        public string Language { get; set; } = "en";

        /// <summary>Semicolon-separated list of priority extensions (e.g. ".pdf;.docx").</summary>
        public string PriorityExtensions { get; set; } = string.Empty;

        /// <summary>Max file size in KB that can transfer simultaneously. 0 = no limit.</summary>
        public long MaxParallelFileSizeKb { get; set; } = 0;

        /// <summary>Log destination. Accepted values: "Local", "Docker", "Both".</summary>
        public string LogDestination { get; set; } = "Local";

        /// <summary>Docker log server URL (e.g. http://localhost:5000/logs). Used when LogDestination is Docker or Both.</summary>
        public string DockerLogUrl { get; set; } = string.Empty;
    }
}