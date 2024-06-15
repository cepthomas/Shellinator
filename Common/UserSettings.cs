using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Splunk.Common
{
    [Serializable]
    public class UserSettings : SettingsCore
    {
        #region Properties - persisted
        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Registry Commands")]
        [Description("Descriptors for context menu commands.")]
        [Browsable(true)]
        public List<RegistryCommand> RegistryCommands { get; set; } = [];
        #endregion
    }
}
