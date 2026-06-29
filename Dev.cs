using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Ephemera.NBagOfTricks;



/* Notes

From MS:
Registry sections of interest:
  - HKEY_LOCAL_MACHINE (HKLM): defaults for all users using a machine (administrator)
  - HKEY_CURRENT_USER (HKCU): user specific settings (not administrator)
  - HKEY_CLASSES_ROOT (HKCR): virtual hive of HKEY_LOCAL_MACHINE with HKEY_CURRENT_USER overrides (administrator)
HKEY_CLASSES_ROOT should be used only for reading currently effective settings. A write to
  HKEY_CLASSES_ROOT is always redirected to HKEY_LOCAL_MACHINE\Software\Classes.
In general, write directly to HKEY_LOCAL_MACHINE\Software\Classes or HKEY_CURRENT_USER\Software\Classes
    and read from HKEY_CLASSES_ROOT (TODO this does not seem to be true?)

- General how to: https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers
- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/

https://learn.microsoft.com/en-us/windows/win32/shell/reg-shell-exts

Nuances of shell command vars:
https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
Ones possibly of interest:
  - %D: Selected file or directory with expanded named folders. Only Dir, File, Folder.
  - %W: The working directory. All except Folder.
  - %L: Selected file or directory name. Only Dir, File.
  - %V: The directory of the selection, maybe unreliable? All except Folder.

This generates registry entries that look like:
  [REG_ROOT\command.RegPath\shell\command.Id]
  @=""
  "MUIVerb"=command.Text
  [REG_ROOT\command.RegPath\shell\Id\command]
  @=command.CommandLine

My commands:
  - HKEY_CLASSES_ROOT\*\shell\run\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\findev\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\openst\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\treex\command
  - HKEY_CLASSES_ROOT\Directory\shell\findev\command
  - HKEY_CLASSES_ROOT\Directory\shell\openst\command
  - HKEY_CLASSES_ROOT\Directory\shell\treex\command

ExplorerContext
Dir => Right click in explorer with a directory selected.
DirBg => Right click in explorer right pane with nothing selected (background).
DeskBg => Right click in windows desktop with nothing selected (background).
File => Right click in explorer with a file selected.
Folder => Seems to appear for any directory selection. Probably meant for system use.
* => Seems to be default if not specified in specific key


i.e. HKEY_CURRENT_USER\Software\Classes\cpp_auto_file
The cpp_auto_file registry key in Windows defines how the system handles C++ source files (files ending in .cpp).
It associates the file extension with the default compiler, IDE, or text editor, like Sublime Text.
[HKEY_CURRENT_USER\Software\Classes\cpp_auto_file]
[HKEY_CURRENT_USER\Software\Classes\cpp_auto_file\shell]
[HKEY_CURRENT_USER\Software\Classes\cpp_auto_file\shell\open]
[HKEY_CURRENT_USER\Software\Classes\cpp_auto_file\shell\open\command]
@="C:\\Program Files\\Sublime Text\\sublime_text.exe \"%1\""

?? File associations:
It used to be that setting the two keys:
    HKCR\.ext (default) = Identifier
    Identifier (default) = "File Description"
        \DefaultIcon (default) = Some icon
        \Shell\Open\Command (default) = Some editor
But now it appears there is an override elsewhere, which is what gets displayed in the Default Programs listing.
HKEY_LOCAL_MACHINE\SOFTWARE\Classes and HKCU\SOFTWARE\Classes
And I don't believe that this has changed recently.
>>>>
Explorer uses a different set of registry keys that can be found at:
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\

*/

namespace Shellinator
{
    internal partial class App
    {
        /// <summary>Do some work.</summary>
        public void DevGo()
        {
            try
            {
                LoadIni(@"C:\Dev\Apps\Shellinator\commands.ini");

                DumpHive(RegistryHive.CurrentUser, @"\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");

                // DumpHive(RegistryHive.CurrentUser, @"Software\Classes");

                // DumpHive(RegistryHive.LocalMachine, @"Software\Classes");


            }
            catch (ShellinatorException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fn"></param>
        /// <exception cref="IniSyntaxException"></exception>
        /// <exception cref="ArgumentException"></exception>
        void LoadIni(string fn)
        {
            // Init runtime values from ini file.
            var inrdr = new IniReader();
            inrdr.ParseFile(fn);

            foreach (var sectName in inrdr.GetSectionNames())
            {
                var nameParts = sectName.SplitByTokens(" ");
                if (nameParts.Count < 2) throw new IniSyntaxException($"Invalid section name [{sectName}]", -1);

                var sectVals = inrdr.GetValues(sectName);


                if (!sectVals.TryGetValue("menu", out var menu))
                {
                    throw new IniSyntaxException($"Missing menu item in section [{sectName}]", -1);
                }
                if (!sectVals.TryGetValue("command", out var command))
                {
                    throw new IniSyntaxException($"Missing command item in section [{sectName}]", -1);
                }

                var ctxt = nameParts[0];
                var key = nameParts[1];

                if (_reserved.Contains(key)) { throw new ArgumentException($"Invalid key: {key}"); }

                switch (ctxt.ToLower())
                {
                    case "dir":
                        _commands2.Add(new(key, ExplorerContext.Dir, menu, command));
                        break;

                    case "dirbg":

                        break;

                    case "deskbg":

                        break;

                    case "file":

                        break;

                    //case "folder":
                    //    break;

                    default:
                        throw new ArgumentException($"Invalid context: {nameParts[0]}");
                        break;

                }

                // ExplorerCommand2(string Key, ExplorerContext Context, string Text, string Command);

                //[DirBg findev]
                //menu = Open here with Everything
                //command = % ProgramFiles %\Everything\everything - parent $target

                //[File bat cmd]
                //menu = Execute
                //command = cmd / C $target


                // ExplorerContext
                // Dir => Right click in explorer with a directory selected.
                // DirBg => Right click in explorer right pane with nothing selected(background).
                // DeskBg => Right click in windows desktop with nothing selected(background).
                // File => Right click in explorer with a file selected.
                // Folder => ?? Seems to appear for any directory selection.Probably meant for system use.


                var vals = inrdr.GetValues("string name");





                // readonly record struct ExplorerCommand2(string Key, ExplorerContext Context, string Text, string Command);

            }

            //var section = inrdr.GetValues("treex");

            //foreach (var val in section)
            //{
            //    switch (val.Key)
            //    {
            //        case "show_files": showFiles = bool.Parse(val.Value); break;
            //        case "show_size": showSize = bool.Parse(val.Value); break;
            //        case "unicode": unicode = bool.Parse(val.Value); break;
            //        case "max_depth": maxDepth = int.Parse(val.Value); break;
            //        case "use_color": color = bool.Parse(val.Value); break;
            //        case "dir_color":
            //            if (!Enum.TryParse(val.Value, true, out dirColor))
            //            { throw new IniSyntaxException($"Invalid color [{val.Value}] for [{val.Key}]", -1); }
            //            break;
            //        case "err_color":
            //            if (!Enum.TryParse(val.Value, true, out errColor))
            //            { throw new IniSyntaxException($"Invalid color [{val.Value}] for [{val.Key}]", -1); }
            //            break;
            //        case "exclude_directories":
            //            var dparts = val.Value.SplitByToken(",");
            //            dparts.ForEach(p => excludeDirectories.Add(p));
            //            break;
            //        case string s when s.Contains("_files"):
            //            var fparts = val.Value.SplitByToken(",");
            //            if (fparts.Count < 2 || !Enum.TryParse(fparts[0], true, out ConsoleColor pclr))
            //            { throw new IniSyntaxException($"Invalid section value [{fparts[0]}] for [{val.Key}]", -1); }
            //            fparts.Take(1..).ForEach(p => extColors.Add(p.Replace(".", ""), pclr));
            //            break;
            //        default: throw new IniSyntaxException($"Invalid section value for [{val.Key}]", -1);
            //    }
            //}
        }

        void DumpHive(RegistryHive hive, string subkey, bool recursive = true)
        {
            Console.WriteLine($"");
            Console.WriteLine($"====================== {hive} {subkey} ======================");
            Console.WriteLine($"");

            using var hkcr = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(subkey, writable: false);

            List<string> contexts = ["Directory", "DesktopBackground", "Folder", "*"]; // "Directory\\Background"

            foreach (var ctx in contexts)
            {
                DoSubkey(regRoot, ctx);
            }

            void DoSubkey(RegistryKey key, string sname, int indent = 0)
            {
                string sind = new(' ', indent * 4);
                Console.WriteLine($"{sind}[{sname}]");

                using var subkey = key.OpenSubKey(sname, writable: false);
                foreach (string sval in subkey.GetValueNames())
                {
                     // "" means default
                     Console.WriteLine($"{sind}  [{sval}]:[{subkey.GetValue(sval)}]");
                }

                if (recursive)
                {
                    // Visit the children.
                    var snames = subkey.GetSubKeyNames();
                    snames.ForEach(s => DoSubkey(subkey, s, indent + 1));
                }
            }
        }
    }
}
