using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Splunk.Common;


namespace Splunk.Ui
{
    [Serializable]
    public class UserSettings : SettingsCore
    {
        #region Properties - persisted
        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Registry Commands")]
        [Description("Descriptors for context menu commands.")]
        [Browsable(true)]
        public List<RegistryCommand> RegistryCommands { get; set; } = [];

        [Browsable(false)]
        public bool Like_Valid_TODO2 { get; set; } = false;
        #endregion
    }
}
