namespace EasySave.Models
{
    /// <summary>
    /// Stores user-configurable application settings persisted in config.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Log file format. Accepted values: "JSON" or "XML".</summary>
        public string LogFormat { get; set; } = "JSON";
    }
}