using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Splunk.Common
{
    /// <summary>
    /// The stuff.
    /// </summary>
    public class Common
    {
        /// <summary>Current global user settings. TODO2 persistence, hook into main.</summary>
        public static UserSettings Settings { get; set; } = new UserSettings();
    }

    [Serializable]
    public class UserSettings : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
        #endregion

        [DisplayName("Registry Commands")]
        [Description("Descriptors for context menu commands.")]
        [Browsable(true)]
        public List<RegistryCommand> RegistryCommands { get; set; } = new();

        #region Properties - internal
        [Browsable(false)]
        public bool Like_Valid_TODO2 { get; set; } = false;
        #endregion
    }
}
