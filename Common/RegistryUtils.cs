using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;


namespace Splunk.Common
{

    public partial class RegistryUtils
    {
        /// <summary>
        /// Command descriptor.
        /// </summary>
        /// <param name="RegPath">Registry key.</param>
        /// <param name="Name">As appears in menu.</param>
        /// <param name="Command">Command to execute.</param>
        public record struct RegCommand(string RegPath, string Name, string Command);

        /// <summary>
        /// Command descriptor.
        /// </summary>
        /// <param name="RegPath"></param>
        /// <param name="Command"></param>
        /// <param name="Name"></param>
        /// <param name="Tag"></param>
        //public record struct RegCommand(string RegPath, string Command, string Name, string Tag);


        [GeneratedRegex(@"[^\w\.@-]", RegexOptions.None)]
        private static partial Regex CleanName();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="splunkPath"></param>
        public static void CreateRegistryEntries(RegCommand[] regCommands, string splunkPath) // path = {clientPath}\Splunk.exe
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var splunk_root = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            foreach (var rc in regCommands)
            {
                string safeName = CleanName().Replace(rc.Name, "_");//, TimeSpan.FromSeconds(1.5));

                var strsubkey = $"{rc.RegPath}\\shell\\{safeName}";

                using (var k1 = splunk_root!.CreateSubKey(strsubkey))
                {
                    Debug.WriteLine($"MUIVerb={rc.Name}");
                    k1.SetValue("MUIVerb", rc.Name);
                }

                strsubkey += "\\command";

                using (var k2 = splunk_root!.CreateSubKey(strsubkey))
                {
                    string expCmd = rc.Command.Replace("%SPLUNK", $"\"{splunkPath}\"");
                    //var scmd = $"\"{exePath}\" {rc.Command} {rc.Tag} \"%V\"";
                    Debug.WriteLine($"@={expCmd}");
                    k2.SetValue("", expCmd);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RemoveRegistryEntries(RegCommand[] regCommands) //TODO1
        {
            //public void DeleteSubKeyTree(string subkey);

        }
    }
}
