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
    }
}
