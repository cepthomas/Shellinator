using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Linq;
using Ephemera.NBagOfTricks;
using System.Collections;


namespace Shellinator
{
    #region Types
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>
    /// Commands vary depending on which part of explorer they originate in. These are supported.
    /// Operations on files are enabled generically, eventually specific extensions could be supported.
    /// </summary>
    enum ExplorerContext
    {
        /// <summary>Right click in explorer with a directory selected.</summary>
        Dir,
        /// <summary>Right click in explorer right pane with nothing selected (background).</summary>
        DirBg,
        /// <summary>Right click in windows desktop with nothing selected (background).</summary>
        DeskBg,
        /// <summary>Right click in explorer with a file selected.</summary>
        File,
        ///// <summary>Seems to appear for any directory selection. Probably meant for system use.</summary>
        //Folder,
    }

    /// <summary>Describes one menu command.</summary>
    /// <param name="Id">Internal id and registry key. Don't use: edit, explore, find, open, print, properties, runas.</param>
    /// <param name="Context">Where to install in `REG_ROOT`</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="Description">As it appears in the context menu.</param>
    /// <param name="Handler">Handle command.</param>
    readonly record struct ExplorerCommand(string Id, ExplorerContext Context, string Text, string Description, CommandHandler Handler);

    /// <summary>Command handler.</summary>
    /// <param name="context">ExplorerContext</param>
    /// <param name="target">Selected item</param>
    delegate ExecResult CommandHandler(ExplorerContext context, string target);

    /// <summary>Convenience container.</summary>
    /// <param name="Code">Return code</param>
    /// <param name="Stdout">stdout if any</param>
    /// <param name="Stderr">stderr if any</param>
    readonly record struct ExecResult(int Code = -1, string Stdout = "", string Stderr = "");
    #endregion

    /// <summary>The generic part of the app.</summary>
    internal partial class App
    {
        #region Fields
        /// <summary>Optional UI.</summary>
        readonly UI _ui = new();

        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        readonly string? _exePath = Environment.GetEnvironmentVariable("TOOLS_PATH");

        /// <summary>Log file path name.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = false;

        /// <summary>All the app commands.</summary>
        List<ExplorerCommand> _commands = [];
        #endregion

        #region The application
        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            // Init stuff.
            if (_exePath is null)
            {
                LogInfo($">>>>> TOOLS_PATH not found, using current directory [{Environment.CurrentDirectory}]");
                _exePath = Environment.CurrentDirectory;
            }

            // Init log.
            _logPath = Path.Join(_exePath, "shellinator.log");
            FileInfo fi = new(_logPath);
            if (fi.Exists && fi.Length > 10000)
            {
                var newfn = _logPath.Replace(".log", "_old.log");
                File.Delete(newfn);
                File.Move(_logPath, newfn);
            }

            InitCommands();

            // Check for running in visual studio. TODO1
            //[VisualStudioDir, C:\Users\cepth\OneDrive\OneDriveDocuments\Visual Studio 2022]
            //[VisualStudioEdition, Microsoft Visual Studio Community 2022]
            //[VisualStudioVersion, 17.0]
            //[VS_Perf_Session_GCHeapCount, 2]
            //[VSAPPIDDIR, C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\]
            //[VSAPPIDNAME, devenv.exe]
            //[VSLANG, 1033]
            //[VsPerMonitorDpiAwarenessEnabled.23744, TRUE]
            //[VSSKUEDITION, Community]

            var evs = Environment.GetEnvironmentVariables();
            foreach (var ev in evs)
            {
                //Debug.WriteLine(ev);
            }

            if (args.Length == 1 && args[0] == "__vsdev__")
            {
                _ui.Show();

                // Dev code.
                _commands.DistinctBy(p => p.Id).ForEach(c => Unreg(c.Id));
                _commands.DistinctBy(p => p.Id).ForEach(c => Reg(c.Id));
                Environment.Exit(0);
            }

            // Process the args => Shellinator.exe id context target.
            try
            {
                LogInfo($"Shellinator command args:{string.Join(" ", args)}");

                _tmit.Snap("Here we go!");

                if (args.Length != 3)
                {
                    throw new ShellinatorException($"Invalid command line format: [{string.Join(" ", args)}]");
                }

                var id = args[0];
                var context = (ExplorerContext)Enum.Parse(typeof(ExplorerContext), args[1]);
                var target = Environment.ExpandEnvironmentVariables(args[2]);

                var cmdProc = _commands.FirstOrDefault(c => c.Id == id); // can throw if invalid

                // Run command.
                var res = cmdProc.Handler(context, target);

                if (res.Code == 0)
                {
                    // Success. Capture any stdout.
                    LogInfo($">>>10 [{res.Stdout == null}]");
                    Clipboard.SetText("res.Stdout");
                }
                else
                {
                    // Failure. Capture everything useful.
                    List<string> ls = [];
                    ls.Append($"=== code: {res.Code}");

                    if (res.Stdout != "")
                    {
                        ls.Append($"=== stdout:");
                        ls.Append($"{res.Stdout}");
                    }

                    if (res.Stderr != "")
                    {
                        ls.Append($"=== stderr:");
                        ls.Append($"{res.Stderr}");
                    }

                    var s = string.Join(Environment.NewLine, ls);
                    LogInfo($">>>20 {string.Join("|", ls)}");
                    Clipboard.SetText(string.Join(Environment.NewLine, ls));
                }
            }
            catch (ShellinatorException ex) // app error
            {
                LogError($"ShellinatorException: {ex.Message}");
                LogInfo(ex.ToString());
                Environment.Exit(1);
            }
            catch (Exception ex) // something else
            {
                LogError($"{ex.GetType()}: {ex.Message}");
                LogInfo(ex.ToString());
                Environment.Exit(2);
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => LogInfo(c));
            Environment.Exit(0);
        }
        #endregion

        #region Internals
        /// <summary>
        /// Generic command executor. Suppresses console window creation.
        /// </summary>
        ///// <param name="exe"></param>
        /// <param name="args"></param>
        /// <param name="cmd"></param>
        /// <returns>Result code, stdout, stderr</returns>
        // ExecResult ExecuteCommand(string exe, List<string> args)
        ExecResult ExecuteCommand(List<string> args, bool cmd = false)
        {
            LogInfo($"ExecuteCommand():[{string.Join(" ", args)}] cmd:{cmd}");

            // This works:
            //var res = ExecuteCommand(["cmd", "/C", "dir", @"C:\Dev\Apps\Treex\Test"]);
            // This fails:
            //var res = ExecuteCommand(["dir", @"C:\Dev\Apps\Treex\Test"]);

            // This works:
            //var res = ExecuteCommand(["cmd", "/C", "treex", @"C:\Dev\Apps\Treex\Test"]);
            // This works:
            //var res = ExecuteCommand(["treex", @"C:\Dev\Apps\Treex\Test"]);

            if (cmd)
            {
                args.InsertRange(0, ["cmd", "/C"]);
            }

            ProcessStartInfo pinfo = new(args[0], args[1..])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Add app-specific environmental variables.
            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };

            proc.Exited += (sender, e) => { LogInfo("Process exit event."); };

            LogInfo("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();

            LogInfo("Wait for process to exit...");
            proc.WaitForExit();

            LogInfo("Exited.");

            return new(proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Write command to the registry. This generates registry entries that look like:
        /// [REG_ROOT\spec.RegPath\shell\spec.Id]
        /// @=""
        /// "MUIVerb"=spec.Text
        /// [REG_ROOT\spec.RegPath\shell\Id\command]
        /// @=spec.CommandLine
        /// </summary>
        /// <param name="id">Which command</param>
        void Reg(string id)
        {
            // Registry sections of interest:
            // - `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
            // - `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
            // - `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)
            // `HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
            // always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes.
            // In general, write directly to `HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.
            // Shellinator bases all registry accesses (R/W) at `HKEY_CURRENT_USER\Software\Classes` aka `REG_ROOT`.
            // - General how to: https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers
            // - Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/

            // Nuances of shell command vars:
            // https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
            // Ones possibly of interest:
            // - %D: Selected file or directory with expanded named folders. Only Dir, File, Folder.
            // - %W: The working directory. All except Folder.
            // - %L: Selected file or directory name. Only Dir, File.
            // - %V: The directory of the selection, maybe unreliable? All except Folder.

            // All paths and macros that expand to paths must be wrapped in double quotes.
            // The builtin env vars like `%ProgramFiles%` are also supported.

            var cmds = _commands.Where(c => c.Id == id);
            if (!cmds.Any())
            {
                throw new ShellinatorException($"Invalid command: {id}");
            }

            foreach (var cmd in cmds)
            {
                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key names etc.
                var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
                var ssubkey2 = $"{ssubkey1}\\command";
                // Assemble command: _exePath id context target

                // Dir:%D DirBg:%W File:%D DeskBg:%W Folder:%D
                var target = cmd.Context switch
                {
                    ExplorerContext.DirBg => "%W",
                    ExplorerContext.DeskBg => "%W",
                    _ => "%D",
                };

                var exec = Path.Join(_exePath, "shellinator.exe");
                var expCmd = $"\"{exec}\" {cmd.Id} {cmd.Context} \"{target}\"";
                expCmd = Environment.ExpandEnvironmentVariables(expCmd);

                if (_fake)
                {
                    LogInfo($"SetValue [{ssubkey1}] -> [MUIVerb={cmd.Text}]");
                    LogInfo($"SetValue [{ssubkey2}] -> [@={expCmd}]");
                }
                else
                {
                    using var k1 = regRoot!.CreateSubKey(ssubkey1);
                    k1.SetValue("MUIVerb", cmd.Text);

                    using var k2 = regRoot!.CreateSubKey(ssubkey2);
                    k2.SetValue("", expCmd);
                }
            }
        }

        /// <summary>Delete existing registry entry.</summary>
        /// <param name="id">Which command</param>
        void Unreg(string id)
        {
            var cmds = _commands.Where(c => c.Id == id);
            if (!cmds.Any())
            {
                throw new ShellinatorException($"Invalid command: {id}");
            }

            foreach (var cmd in cmds)
            {
                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key name.
                var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";

                if (_fake)
                {
                    LogInfo($"DeleteSubKeyTree [{ssubkey}]");
                }
                else
                {
                    regRoot!.DeleteSubKeyTree(ssubkey, false);
                }
            }
        }

        /// <summary>Convert the context enum to registry key.</summary>
        static string GetRegPath(ExplorerContext context)
        {
            return context switch
            {
                ExplorerContext.Dir => "Directory",
                ExplorerContext.DirBg => "Directory\\Background",
                ExplorerContext.DeskBg => "DesktopBackground",
                ExplorerContext.File => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }

        /// <summary>Simple logging and notification.</summary>
        void LogInfo(string msg)
        {
            // Always log.
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {msg}{Environment.NewLine}");

            if (_ui.Visible)
            {
                _ui.AppendLine(msg);
            }
        }

        /// <summary>Simple logging and notification.</summary>
        void LogError(string msg)
        {
            // Always log.
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} ERROR {msg}{Environment.NewLine}");

            if (_ui.Visible)
            {
                _ui.AppendLine(msg);
            }

            MessageBox.Show(msg);
        }
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new App(args);
        }
    }
}
