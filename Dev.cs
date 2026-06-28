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

All commands:
  - HKEY_CLASSES_ROOT\*\shell\run\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\findev\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\openst\command
  - HKEY_CLASSES_ROOT\Directory\Background\shell\treex\command
  - HKEY_CLASSES_ROOT\Directory\shell\findev\command
  - HKEY_CLASSES_ROOT\Directory\shell\openst\command
  - HKEY_CLASSES_ROOT\Directory\shell\treex\command
*/

namespace Shellinator
{
    internal class Dev
    {
        /// <summary>Do some work.</summary>
        public void Go()
        {
            try
            {
                DumpHive(RegistryHive.CurrentUser);

                DumpHive(RegistryHive.LocalMachine);

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

        void DumpHive(RegistryHive hive)
        {
            Console.WriteLine($"============================== {hive} ==============================");

            using var hkcr = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(@"Software\Classes", writable: false);

            List<string> contexts = ["Directory", "Directory\\Background", "DesktopBackground", "*", "Folder"];

            foreach (var ctx in contexts)
            {
                DoSubkey(regRoot, ctx, 0);
            }

            void DoSubkey(RegistryKey key, string sname, int indent)
            {
                string sind = new(' ', indent * 2);
                Console.WriteLine($"{sind}[{sname}]");

                using var subkey = key.OpenSubKey(sname, writable: false);
                foreach (string sval in subkey.GetValueNames())
                {
                     // "" means default
                     Console.WriteLine($"{sind}  [{sval}]:[{subkey.GetValue(sval)}]");
                }

                // Visit the children.
                var snames = subkey.GetSubKeyNames();
                snames.ForEach(s => DoSubkey(subkey, s, indent + 1));
            }
        }
    }
}
