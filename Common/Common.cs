using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace Splunk.Common
{
    /// <summary>
    /// The stuff.
    /// </summary>
    public class Common
    {
        /// <summary>Current global user settings.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();

        /// <summary>Client/server comm id.</summary>
        public static string PipeName { get { return "BD7A5E0A-DA11-40AC-AF94-950674716136"; } }

        /// <summary>Shared log file.</summary>
        public static string LogFileName { get { return MiscUtils.GetAppDataDir("Splunk", "Ephemera") + @"\splunk.txt"; } }
    }

    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        // #region Persisted editable properties
        // [DisplayName("Auto Close")]
        // [Description("Automatically close after playing the file.")]
        // [Browsable(true)]
        // public bool AutoClose { get; set; } = true;

        // [DisplayName("File Log Level")]
        // [Description("Log level for UI notification.")]
        // [Browsable(true)]
        // [JsonConverter(typeof(JsonStringEnumConverter))]
        // public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
        // #endregion

        // #region Persisted Non-editable Properties
        // [Browsable(false)]
        // public double Volume { get; set; } = 0.7;
        // #endregion
    }
}
