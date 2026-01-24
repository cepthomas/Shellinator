using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Linq;
using System.Collections;
using Ephemera.NBagOfTricks;
using System.Runtime.CompilerServices;


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
        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        readonly string _exePath;

        /// <summary>Log file path name.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = false;

        /// <summary>All the app commands.</summary>
        List<ExplorerCommand> _commands = [];
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            new App(args);
        }

        #region The command line application
        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            int code = 0;

            try
            {
                ///// Init internal stuff.

                // Standard path supplied?
                var toolsPath = Environment.GetEnvironmentVariable("TOOLS_PATH");
                if (toolsPath is null) { throw new ShellinatorException("Environment missing TOOLS_PATH"); }

                _exePath = Path.Combine(toolsPath, "Apps");
                if (!Path.Exists(_exePath)) { throw new ShellinatorException($"Missing folder {_exePath}"); }

                // Init log.
                _logPath = Path.Join(_exePath, "shellinator.log");
                FileInfo fi = new(_logPath);
                if (fi.Exists && fi.Length > 10000)
                {
                    var newfn = _logPath.Replace(".log", "_old.log");
                    File.Delete(newfn);
                    File.Move(_logPath, newfn);
                }

                ///// Set up commands.
                InitCommands();

                ///// Check for running in visual studio. That indicates mode.
                if (Environment.GetEnvironmentVariable("VisualStudioVersion") is not null)
                {
                    // VS management mode. Default is to rewrite the commands to the registry.
                    //_commands.DistinctBy(p => p.Id).ForEach(c => Unreg(c.Id, toolsPath));
                    //_commands.DistinctBy(p => p.Id).ForEach(c => Reg(c.Id, _exePath));
                }
                else
                {
                    // Normal mode called from system/registry.
                    LogInfo($"Shellinator command args:{string.Join(" ", args)}");

                    _tmit.Snap("Here we go!");

                    // Process the args: shellinator.exe id context target.
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
                        // Success. Capture any stdout. TIL don't set clipboard to an empty string.
                        if (res.Stdout.Length > 0)
                        {
                            Clipboard.SetText(res.Stdout);
                        }
                        else
                        {
                            Clipboard.Clear();
                        }
                    }
                    else
                    {
                        // Command failed. Capture everything useful.
                        List<string> ls = [];
                        ls.Add($"=== code: {res.Code}");

                        if (res.Stdout.Length > 0)
                        {
                            ls.Add($"=== stdout:");
                            ls.Add($"{res.Stdout}");
                        }

                        if (res.Stderr.Length > 0)
                        {
                            ls.Add($"=== stderr:");
                            ls.Add($"{res.Stderr}");
                        }

                        var sres = string.Join(Environment.NewLine, ls);
                        LogError(sres);

                        Clipboard.SetText(sres);
                        code = 1;
                    }
                }
            }
            catch (ShellinatorException ex) // app error
            {
                LogError($"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 2;
            }
            catch (Exception ex) // something else
            {
                LogError($"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 3;
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => LogInfo(c));
            
            Environment.Exit(code);
        }
        #endregion

        #region Internals
        /// <summary>
        /// Generic command executor. Called by commands handlers. Suppresses console window creation.
        /// </summary>
        /// <param name="args">All args including command first.</param>
        /// <param name="cmd">True for command line commands. Wraps the call in 'cmd /C'.</param>
        /// <returns>Result code, stdout, stderr</returns>
        ExecResult ExecuteCommand(List<string> args, bool cmd = false)
        {
            LogInfo($"ExecuteCommand():[{string.Join(" ", args)}] cmd:{cmd}");

            if (cmd)
            {
                args.InsertRange(0, ["cmd", "/C"]);
            }

            ProcessStartInfo pinfo = new(args[0], args[1..])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Add app-specific environmental variables.
            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };
            //proc.Exited += (sender, e) => { LogInfo("Process exit event."); };

            LogInfo("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            //LogInfo("Wait for process to exit...");
            proc.WaitForExit();
            LogInfo("Exited.");

            return new(proc.ExitCode, stdout, stderr);
        }
        #endregion

        #region Registry editing
        /// <summary>
        /// Write command to the registry. This generates registry entries that look like:
        /// [REG_ROOT\spec.RegPath\shell\spec.Id]
        /// @=""
        /// "MUIVerb"=spec.Text
        /// [REG_ROOT\spec.RegPath\shell\Id\command]
        /// @=spec.CommandLine
        /// </summary>
        /// <param name="id">Which command</param>
        /// <param name="path">Exe location</param>
        void Reg(string id, string path)
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
                // Assemble command: _exePath id context target

                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key names etc.
                var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
                var ssubkey2 = $"{ssubkey1}\\command";

                // Determine target based on origin. Dir/File/Folder:%D DirBg/DeskBg:%W.
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
        /// <param name="path">Exe location</param>
        void Unreg(string id, string path)
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

        /// <summary>Convert the internal enum to registry key.</summary>
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
        #endregion

        #region Common infrastructure
        /// <summary>Simple logging and notification.</summary>
        void LogInfo(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            // Always log.
            var fspec = file != "" ? $"{Path.GetFileName(file)}({line}) " : " ";
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {fspec}{msg}{Environment.NewLine}");
        }

        /// <summary>Simple logging and notification.</summary>
        void LogError(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            // Always log.
            var fspec = file != "" ? $"{Path.GetFileName(file)}({line}) " : " ";
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} ERROR {fspec}{msg}{Environment.NewLine}");
            MessageBox.Show($"{fspec}{msg}", "Command failed");
        }
        #endregion
    }
}
