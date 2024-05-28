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


namespace Splunk.Ui
{
    [Serializable]
    public sealed class Settings : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("Script Path")]
        [Description("Default location for user scripts.")]
        [Browsable(true)]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string ScriptPath { get; set; } = "";

        [DisplayName("Auto Compile")]
        [Description("Compile current file when change detected.")]
        [Browsable(true)]
        public bool AutoCompile { get; set; } = true;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
        #endregion

        public List<Command> Commands { get; set; } = new();

        #region Properties - internal
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;
        #endregion
    }

    [Serializable]
    public sealed class Commands
    {
        #region Properties - persisted editable
        [DisplayName("Script Path")]
        [Description("Default location for user scripts.")]
        [Browsable(true)]
        public string Command { get; set; } = "";
        #endregion
    }
}
