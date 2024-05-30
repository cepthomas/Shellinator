using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;



namespace Splunk.Common
{
    [Serializable]
    public class RegistryCommand
    {
        #region Properties - persisted editable
        [DisplayName("Identifier")]
        [Description("Short name for internal id and registry key.")]
        [Browsable(true)]
        public string Id { get; set; } = "";

        [DisplayName("Registry Path")]
        [Description("Where to install in HKCU as Key\\Subkey\\...")]
        [Browsable(true)]
        public string RegPath { get; set; } = "";

        [DisplayName("Text")]
        [Description("As it appears in the context menu.")]
        [Browsable(true)]
        public string Text { get; set; } = "";

        [DisplayName("Command")]
        [Description("Full command string to execute.")]
        [Browsable(true)]
        public string CommandLine { get; set; } = "";

        [DisplayName("Description")]
        [Description("Info about this command. TODO2 add values")]
        [Browsable(true)]
        public string Description { get; set; } = "";

        // TODO2 Attributes like icon, position, extended, ...
        #endregion

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public RegistryCommand()
        {
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="regPath"></param>
        /// <param name="text"></param>
        /// <param name="cmdLine"></param>
        /// <param name="desc"></param>
        public RegistryCommand(string id, string regPath, string text, string cmdLine, string desc = "")
        {
            Id = id;
            RegPath = regPath;
            Text = text;
            CommandLine = cmdLine;
            Description = desc;
        }

        public override string ToString()
        {
            return $"{Id}: {Text}";
        }
    }

    public class RegistryUtils
    {
        static readonly bool _fake = true;

        /// <summary>
        /// Write one command to the registry.
        /// </summary>
        /// <param name="splunkPath"></param>
        public static void CreateRegistryEntry(RegistryCommand rc, string splunkPath)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{rc.RegPath}\\shell\\{rc.Id}";
            var ssubkey2 = $"{ssubkey1}\\command";
            var expCmd = rc.CommandLine.Replace("%SPLUNK", $"\"{splunkPath}\"").Replace("%ID", rc.Id);

            if (_fake)
            {
                Debug.WriteLine($"Create [{ssubkey1}]  MUIVerb={rc.Text}");
                Debug.WriteLine($"Create [{ssubkey2}]  @={expCmd}");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(ssubkey1);
                k1.SetValue("MUIVerb", rc.Text);

                using var k2 = regRoot!.CreateSubKey(ssubkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RemoveRegistryEntry(RegistryCommand rc)
        {
            //public void DeleteSubKeyTree(string subkey);
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{rc.RegPath}\\shell\\{rc.Id}";

            if (_fake)
            {
                Debug.WriteLine($"Delete [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey);
            }
        }
    }
}
