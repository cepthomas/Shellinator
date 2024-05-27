using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using System.Diagnostics;


namespace Splunk.Common
{

    public class RegistryUtils
    {
        public record struct RegCommand(string RegPath, string Command, string Name, string Tag);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientPath"></param>
        public static void CreateRegistryEntries(RegCommand[] regCommands, string exePath) // path = {clientPath}\Splunk.exe
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var splunk_root = hkcu.OpenSubKey(@"Software\Classes", writable: true);
            foreach (var rc in regCommands)
            {
                var strsubkey = $"{rc.RegPath}\\shell\\{rc.Command}";

                using (var k = splunk_root!.CreateSubKey(strsubkey))
                {
                    Debug.WriteLine($"MUIVerb={rc.Name}");
                    k.SetValue("MUIVerb", rc.Name);
                }

                strsubkey += "\\command";

                using (var k = splunk_root!.CreateSubKey(strsubkey))
                {
                    //cmd.exe /s /k pushd "%V"
                    //"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe" "%1" source:Explorer

                    var scmd = $"\"{exePath}\" {rc.Command} {rc.Tag} \"%V\"";
                    Debug.WriteLine($"@={scmd}");
                    k.SetValue("", scmd);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RemoveRegistryEntries() //TODO1
        {
            //public void DeleteSubKeyTree(string subkey);

        }
    }
}
