using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;
//using Microsoft.Win32;

//%l – Long file name form of the first parameter. Note that Win32/64 applications will be passed the long file name, whereas Win16 applications get the short file name.Specifying %l is preferred as it avoids the need to probe for the application type.
//%d – Desktop absolute parsing name of the first parameter (for items that don't have file system paths).
//%v – For verbs that are none implies all.If there is no parameter passed this is the working directory.
//%V should be used if you want directory name, ie.when you want to add your command on context menu when
//  you click on background, not on a single file or a directory name. %L won't work in that case.
//%w – The working directory.
//A warning about %W: It is not always available and will throw a cryptic error message if used in your command value.For example, calling your context menu item on a drive's or a library folder's context menu will not initialize this variable.Avoid its use outside of a file handler's context menu entry.
//%* – Replace with all parameters.
//%~ – Replace with all parameters starting with and following the second parameter.
//%0 or %1 – The first file parameter.For example "C:\Users\Eric\Desktop\New Text Document.txt". Generally this should be in quotes and the applications command line parsing should accept quotes to disambiguate files with spaces in the name and different command line parameters (this is a security best practice and I believe mentioned in MSDN).
//%<n> (where<n> is 2-9) – Replace with the nth parameter.
//%s – Show command.

namespace Splunk.Common
{

    [Serializable]
    public class RegistryCommand
    {
        #region Properties - persisted editable
        [DisplayName("Identifier")]
        [Description("Short name for internal id and registry key")]
        [Browsable(true)]
        public string Id { get; set; } = "";

        [DisplayName("Registry Path")]
        [Description("Where to install - as Key\\Subkey\\...")]
        [Browsable(true)]
        public string RegPath { get; set; } = "";

        [DisplayName("Text")]
        [Description("Name as it appears in the context menu.")]
        [Browsable(true)]
        public string Text { get; set; } = "";

        [DisplayName("Command")]
        [Description("Full command string to execute.")]
        [Browsable(true)]
        public string Command { get; set; } = "";
        #endregion

        public RegistryCommand(string id, string regPath, string text, string command)
        {
            Id = id;
            RegPath = regPath;
            Text = text;
            Command = command;
        }
    }

    public class RegistryUtils
    {
        /// <summary>
        /// Command descriptor.
        /// </summary>
        /// <param name="RegPath">Registry key.</param>
        /// <param name="Name">As appears in menu.</param>
        /// <param name="Command">Command to execute.</param>
        //public record struct RegCommand(string RegPath, string Name, string Command);

        /// <summary>
        /// Command descriptor.
        /// </summary>
        /// <param name="RegPath"></param>
        /// <param name="Command"></param>
        /// <param name="Name"></param>
        /// <param name="Tag"></param>
        //public record struct RegCommand(string RegPath, string Command, string Name, string Tag);


        //[GeneratedRegex(@"[^\w\.@-]", RegexOptions.None)]
        //private static partial Regex CleanName();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="splunkPath"></param>
        public static void CreateRegistryEntry(RegistryCommand rc, string splunkPath) // path = {clientPath}\Splunk.exe
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var splunk_root = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            var strsubkey = $"{rc.RegPath}\\shell\\{rc.Id}";

            using (var k1 = splunk_root!.CreateSubKey(strsubkey))
            {
                //Debug.WriteLine($"MUIVerb={rc.Text}");
                k1.SetValue("MUIVerb", rc.Text);
            }

            strsubkey += "\\command";

            using (var k2 = splunk_root!.CreateSubKey(strsubkey))
            {
                string expCmd = rc.Command.Replace("%SPLUNK", $"\"{splunkPath}\"").Replace("%ID", rc.Id);
                //Debug.WriteLine($"@={expCmd}");
                k2.SetValue("", expCmd);
            }

            /* Doc for editor
            [HKEY_XX\Directory\shell\menu_item]
            Right click in explorer-right-pane or windows-desktop with a directory selected.
            %V is the directory.
            XX TAG is "dir"

            [HKEY_XX\Directory\Background\shell\menu_item]
            Right click in explorer-right-pane with nothing selected (background)
            %V is not used.
            XX TAG is "dirbg"

            [HKEY_XX\DesktopBackground\shell\menu_item]
            Right click in windows-desktop with nothing selected (background).
            %V is not used.
            XX TAG is "deskbg"

            [HKEY_XX\Folder\shell\menu_item]
            Right click in explorer-left-pane (navigation) with a folder selected.
            %V is the folder.
            XX TAG is "folder"

            [HKEY_XX\*\shell\menu_item]
            ; => Right click in explorer-right-pane or windows-desktop with a file selected (* for all exts).
            %V is the file name.
            XX TAG is "file"
            */
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RemoveRegistryEntry(RegistryCommand regCommand) //TODO1
        {
            //public void DeleteSubKeyTree(string subkey);

        }
    }
}
