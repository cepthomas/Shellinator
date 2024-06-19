using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json.Serialization;


namespace Splunk.Ui
{
    /// <summary>See README#Commands. File to support specific extensions?</summary>
    public enum ExplorerContext { Dir, DirBg, DeskBg, Folder, File }

    [Serializable]
    public class ExplorerCommand
    {
        #region Fields
        /// <summary>Dry run the registry writes.</summary>
        static readonly bool _fake = false;
        #endregion

        #region Properties - persisted editable
        [DisplayName("Identifier")]
        [Description("Short name for internal id and registry key.")]
        [Browsable(true)]
        public string Id { get; init; } = "???";

        [DisplayName("Explorer Context")]
        [Description("Explorer context menu origin.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExplorerContext Context { get; init; } = ExplorerContext.Dir;

        [DisplayName("Text")]
        [Description("As it appears in the context menu.")]
        [Browsable(true)]
        public string Text { get; init; } = "???";

        [DisplayName("Command Line")]
        [Description("Full command string to execute.")]
        [Browsable(true)]
        public string CommandLine { get; init; } = "";

        [DisplayName("Description")]
        [Description("Info about this command.")]
        [Browsable(true)]
        public string Description { get; init; } = "";
        #endregion

        /// <summary>Default constructor for serialization.</summary>
        public ExplorerCommand()
        {
        }

        /// <summary>Normal constructor.</summary>
        /// <param name="id"></param>
        /// <param name="context"></param>
        /// <param name="text"></param>
        /// <param name="cmdLine"></param>
        /// <param name="desc"></param>
        public ExplorerCommand(string id, ExplorerContext context, string text, string cmdLine, string desc)
        {
            var ss = new List<string> { "edit", "explore", "find", "open", "print", "properties", "runas" };
            if (ss.Contains(id)) { throw new ArgumentException($"Reserved id:{id}"); }

            Id = id;
            Context = context;
            Text = text;
            CommandLine = cmdLine;
            Description = desc;
        }

        /// <summary>Write command to the registry.</summary>
        /// <param name="splunkPath"></param>
        public void CreateRegistryEntry(string splunkPath)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{GetRegPath(Context)}\\shell\\{Id}";
            var ssubkey2 = $"{ssubkey1}\\command";
            var expCmd = CommandLine.Replace("%SPLUNK", $"\"{splunkPath}\"").Replace("%ID", Id);
            expCmd = Environment.ExpandEnvironmentVariables(expCmd);

            if (_fake)
            {
                Debug.WriteLine($"Create [{ssubkey1}]  MUIVerb={Text}");
                Debug.WriteLine($"Create [{ssubkey2}]  @={expCmd}");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(ssubkey1);
                k1.SetValue("MUIVerb", Text);

                using var k2 = regRoot!.CreateSubKey(ssubkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>Delete registry entry.</summary>
        public void RemoveRegistryEntry()
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(Context)}\\shell\\{Id}";

            if (_fake)
            {
                Debug.WriteLine($"Delete [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey);
            }
        }

        /// <summary>Readable version for property grid label.</summary>
        public override string ToString()
        {
            return $"{Id}: {Text}";
        }

        /// <summary>Convert the splunk context to registry key.</summary>
        public static string GetRegPath(ExplorerContext context)
        {
            return context switch
            {
                ExplorerContext.Dir => "Directory",
                ExplorerContext.DirBg => "Directory\\Background",
                ExplorerContext.DeskBg => "DesktopBackground",
                ExplorerContext.Folder => "Folder",
                ExplorerContext.File => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }
    }
}
