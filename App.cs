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

//TODO1 service: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service


namespace Shellinator
{
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
        readonly bool _fake = true; //false;

        /// <summary>Don't use reserved commands.</summary>
        readonly List<string> _reserved = ["edit", "explore", "find", "open", "print", "properties", "runas"];

        // /// <summary>All the app commands.</summary>
        // List<ExplorerCommand> _commands = [];

        /// <summary>New flavor - All the app commands.</summary>
        readonly List<ExplorerCommand_Nuevo> _commands_Nuevo = [];

        /// <summary>Determine mode.</summary>
        bool _inDev = false;
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
        /// <param name="appArgs"></param>
        public App(string[] appArgs)
        {
            int code = 0;

            try
            {
                _tmit.Snap("App constructor");

                ///// Init internal stuff.
                _inDev = Debugger.IsAttached; // or look through Process.GetCurrentProcess().Modules

                // Standard path supplied?
                var toolsPath = Environment.GetEnvironmentVariable("TOOLS_PATH") ?? throw new ShellinatorException("Environment missing TOOLS_PATH");
                _exePath = toolsPath;
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

                // Set up commands.
                // old style
                //InitCommands();

                // _Nuevo style. Config file is assumed to be next to the executable. TODO1 init a default
                _tmit.Snap("Start load config");
                var dir = Path.GetDirectoryName(Application.ExecutablePath);
                var cfn = Path.Combine(dir!, _inDev ? "default.ini" : "shellinator.ini");
                LoadIni(cfn);
                _tmit.Snap("Done load config");

                ///// Process the args: shellinator.exe id context target.
               // string func = appArgs.Length > 0 ? appArgs[0].ToLower() : "No args!";

                var command = appArgs.Length > 0 ? appArgs[0].ToLower() : "";
                var context = appArgs.Length > 1 ? appArgs[1].ToLower() : null;
                var target = appArgs.Length > 2 ? appArgs[2] : null;


                switch (command, context, target)
                {
                    case ("reg", null, null):
                        RegisterAll_Nuevo();
                        break;

                    case ("unreg", null, null):
                        UnregisterAll_Nuevo();
                        break;

                    case ("dev", null, null):
                        LogInfo($"Shellinator dev mode");
                        ///// New dev code. /////
                        //LoadIni(@"C:\Dev\Apps\Shellinator\commands.ini");

                        //var dump = DumpHive(RegistryHive.CurrentUser, @"Software\Classes");
                        //// DumpHive(RegistryHive.LocalMachine, @"Software\Classes");
                        //// DumpHive(RegistryHive.CurrentUser, @"\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");
                        //dump.ForEach(x => Console.WriteLine(x));

                        ///// Original dev code. /////
                        //_fake = true;
                        //LogInfo($"Shellinator register all");
                        //_commands.DistinctBy(p => p.Id).ForEach(c => Reg(c.Id));
                        //LogInfo($"Shellinator unregister all");
                        //_commands.DistinctBy(p => p.Id).ForEach(c => Unreg(c.Id));
                        //var tcmd = TestCmd(ExplorerContext.Dir, "Do stuff...");
                        break;

                    case (_, "dir", not null):
                    case (_, "dirbg", not null):
                    case (_, "deskbg", not null):

                        //    readonly record struct ExplorerCommand_Nuevo(ExplorerContext Context, string Command, string Text, string ExecLine);

                        var cmds = _commands_Nuevo.Where(c => c.Context == context && c.Command == command);
                        if (!cmds.Any())
                        {
                            LogError($"Invalid");
                        }
                        else
                        {

                        }

                        var res = cmdProc.Handler(context, target);



                        break;


                    case (_, "file", not null):
                        // Assume normal mode called from system using registry entry.

                        // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\findev]
                        // "MUIVerb"="Open in Everything"
                        // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\findev\command]
                        // @="\"C:\\Users\\cepth\\OneDrive\\Tools\\Apps\\shellinator.exe\" findev DirBg \"%W\""

                        // [HKEY_CURRENT_USER\Software\Classes\Directory\shell\findev]
                        // "MUIVerb"="Open in Everything"
                        // [HKEY_CURRENT_USER\Software\Classes\Directory\shell\findev\command]
                        // @="\"C:\\Users\\cepth\\OneDrive\\Tools\\Apps\\shellinator.exe\" findev Dir \"%D\""

                        // [HKEY_CURRENT_USER\Software\Classes\*\shell\run]
                        // "MUIVerb"="Run"
                        // [HKEY_CURRENT_USER\Software\Classes\*\shell\run\command]
                        // @="\"C:\\Users\\cepth\\OneDrive\\Tools\\Apps\\shellinator.exe\" run File \"%D\""


                        // @="\"C:\\Users\\cepth\\OneDrive\\Tools\\Apps\\shellinator.exe\" findev DirBg \"%W\""
                        // findev DirBg \"%W\""
                        _tmit.Snap("Execute command:");
                        LogInfo($"Shellinator command args [{string.Join("|", appArgs)}]");


                        // var context = (ExplorerContext)Enum.Parse(typeof(ExplorerContext), args[1]);
                        // var target = Environment.ExpandEnvironmentVariables(args[2]);
                        // var cmdProc = _commands_Nuevo.FirstOrDefault(c => c.Id == id);
                        // // Run command.
                        // var res = cmdProc.Handler(context, target);

                        // readonly record struct ExplorerCommand_Nuevo(ExplorerContext Context, string Arg, string Text, string CommandLine);

                        // Find and run command.
                        //var cmd = _commands_Nuevo.FirstOrDefault(c => c.Arg == arg);



                        // [Dir demo]
                        // menu = Do something with this dir!
                        // command = start cmd /C "echo Dir demo $target on %COMPUTERNAME% && pause"
                        // [File bat cmd]
                        // menu = Execute
                        // command = cmd /C $target



                        if (res.Code == 0)
                        {
                            // Success. Capture any stdout. TIL don't set clipboard to an empty string.
                            Clipboard.SetText(res.Stdout.Length > 0 ? res.Stdout : "Success");
                        }
                        else
                        {
                            // Command failed. Capture everything useful.
                            List<string> ls = [];
                            ls.Add($">>> FAILED!!!{Environment.NewLine}code: {res.Code}");
                            ls.Add($">>> stdout:{Environment.NewLine}{(res.Stdout.Length > 0 ? res.Stdout : "None")}");
                            ls.Add($">>> stderr:{Environment.NewLine}{(res.Stderr.Length > 0 ? res.Stderr : "None")}");
                            var sres = string.Join(Environment.NewLine, ls);
                            LogError(sres);
                            Clipboard.SetText(sres);
                            code = 1;
                        }
                        break;

                    default:
                        throw new ShellinatorException($"Invalid command line args: [{string.Join(" ", appArgs)}]");
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
            _tmit.Captures.ForEach(c => LogInfo($"TMIT [{c}]"));

            Environment.Exit(code);
        }
        #endregion

        #region Internals
        /// <summary>
        /// 
        /// </summary>
        void RegisterAll_Nuevo()
        {
            LogInfo($"Shellinator register all"); // TODO1 keep a list of installed keys so unreg is cleaner.
            UnregisterAll_Nuevo(); // clean up first

            _commands_Nuevo.ForEach(cmd => { Register_Nuevo(cmd); });
        }

        /// <summary>
        /// 
        /// </summary>
        void UnregisterAll_Nuevo()
        {
            LogInfo($"Shellinator unregister all");

            _commands_Nuevo.ForEach(cmd => { Unregister_Nuevo(cmd); });
        }

        /// <summary>
        /// Generic command executor. Called by command handlers. Suppresses console window creation.
        /// </summary>
        /// <param name="args">All args including command first.</param>
        /// <param name="cmd">True for command line commands. Wraps the call in 'cmd /C'.</param>
        /// <returns>Result code, stdout, stderr</returns>
        ExecResult ExecuteCommand(List<string> args, bool cmd = false) //TODO1
        {
            //LogInfo($"ExecuteCommand():[{string.Join(" ", args)}] cmd:{cmd}");

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

            //LogInfo("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            // LogInfo("Wait for process to exit...");
            proc.WaitForExit();
            //LogInfo("Exited.");

            return new(proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Load an ini config file.
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
                var sectionParts = sectName.SplitByTokens(" ");
                if (sectionParts.Count < 2) throw new IniSyntaxException($"Invalid section name [{sectName}]", -1);

                var ctxt = sectionParts[0];
                var id = sectionParts[1];
                var sectVals = inrdr.GetValues(sectName);

                if (!sectVals.TryGetValue("menu", out var menu)) throw new IniSyntaxException($"Missing menu item in section [{sectName}]", -1);
                if (!sectVals.TryGetValue("command", out var command)) throw new IniSyntaxException($"Missing command item in section [{sectName}]", -1);
                if (_reserved.Contains(id)) { throw new ArgumentException($"Reserved key [{id}]"); }
                switch (ctxt.ToLower())
                {
                    case "dir":
                        _commands_Nuevo.Add(new(ExplorerContext.Dir, id, menu, command));
                        break;

                    case "dirbg":
                        _commands_Nuevo.Add(new(ExplorerContext.DirBg, id, menu, command));
                        break;

                    case "deskbg":
                        _commands_Nuevo.Add(new(ExplorerContext.DeskBg, id, menu, command));
                        break;

                    case "file":
                        sectionParts[1..].ForEach(p => _commands_Nuevo.Add(new(ExplorerContext.File, p, menu, command)));
                        break;

                    //case "folder":
                    //    break;

                    default:
                        throw new ArgumentException($"Invalid context: {sectionParts[0]}");
                }
            }
        }
        #endregion




        /// <summary>
        /// Write a command to the registry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        void Register_Nuevo(ExplorerCommand_Nuevo cmd)
        {
            // This generates registry entries that look like:
            // HKEY_CURRENT_USER\Software\Classes\<command-reg-path>\shell\<cmd.Id>
            // "MUIVerb"="<cmd.Text>"
            // HKEY_CURRENT_USER\Software\Classes\<command-reg-path>\shell\Id\command
            // @="<cmd.CommandLine>"


            // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell]
            // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\findev]
            // "MUIVerb"="Open in Everything"
            // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\findev\command]
            // @="\"C:\\Users\\cepth\\OneDrive\\Tools\\Apps\\shellinator.exe\" findev DirBg \"%W\""

            // readonly record struct ExplorerCommand_Nuevo(ExplorerContext Context, string Id, string Text, string CommandLine);



            // Assemble command: _exePath id context target

            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Command}";
            var ssubkey2 = $"{ssubkey1}\\command";

            // Determine target based on origin. Dir/File/Folder:%D DirBg/DeskBg:%W.
            var target = cmd.Context switch
            {
                ExplorerContext.DirBg => "%W",
                ExplorerContext.DeskBg => "%W",
                _ => "%D",
            };

            // All paths and macros that expand to paths must be wrapped in double quotes.
            // The builtin env vars like %ProgramFiles% are also supported.
            var exec = Path.Join(_exePath, "shellinator.exe");
            var expCmd = $"\"{exec}\" {cmd.Command} {cmd.Context} \"{target}\"";
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

        /// <summary>Delete existing registry entry.</summary>
        /// <param name="cmd">Which command</param>
        void Unregister_Nuevo(ExplorerCommand_Nuevo cmd)// string id)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Command}";

            if (_fake)
            {
                LogInfo($"DeleteSubKeyTree [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey, false);
            }
        }










        #region Registry editing
        // /// <summary>
        // /// Write a command to the registry.
        // /// </summary>
        // /// <param name="id">Which command</param>
        // void Register(string id)
        // {
        //     // This generates registry entries that look like:
        //     // [REG_ROOT\command.RegPath\shell\command.Id]
        //     // @=""
        //     // "MUIVerb"=command.Text
        //     // [REG_ROOT\command.RegPath\shell\Id\command]
        //     // @=command.CommandLine

        //     var cmds = _commands.Where(c => c.Id == id);
        //     if (!cmds.Any()) { throw new ShellinatorException($"Invalid command: {id}"); }
        //     if (_reserved.Contains(id)) { throw new ArgumentException($"Invalid command: {id}"); }

        //     foreach (var cmd in cmds)
        //     {
        //         // Assemble command: _exePath id context target

        //         using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
        //         using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

        //         // Key names etc.
        //         var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
        //         var ssubkey2 = $"{ssubkey1}\\command";

        //         // Determine target based on origin. Dir/File/Folder:%D DirBg/DeskBg:%W.
        //         var target = cmd.Context switch
        //         {
        //             ExplorerContext.DirBg => "%W",
        //             ExplorerContext.DeskBg => "%W",
        //             _ => "%D",
        //         };

        //         // All paths and macros that expand to paths must be wrapped in double quotes.
        //         // The builtin env vars like %ProgramFiles% are also supported.
        //         var exec = Path.Join(_exePath, "shellinator.exe");
        //         var expCmd = $"\"{exec}\" {cmd.Id} {cmd.Context} \"{target}\"";
        //         expCmd = Environment.ExpandEnvironmentVariables(expCmd);

        //         if (_fake)
        //         {
        //             LogInfo($"SetValue [{ssubkey1}] -> [MUIVerb={cmd.Text}]");
        //             LogInfo($"SetValue [{ssubkey2}] -> [@={expCmd}]");
        //         }
        //         else
        //         {
        //             using var k1 = regRoot!.CreateSubKey(ssubkey1);
        //             k1.SetValue("MUIVerb", cmd.Text);

        //             using var k2 = regRoot!.CreateSubKey(ssubkey2);
        //             k2.SetValue("", expCmd);
        //         }
        //     }
        // }

        // /// <summary>Delete existing registry entry.</summary>
        // /// <param name="id">Which command</param>
        // void Unregister(string id)
        // {
        //     var cmds = _commands.Where(c => c.Id == id);
        //     if (!cmds.Any())
        //     {
        //         throw new ShellinatorException($"Invalid command: {id}");
        //     }

        //     foreach (var cmd in cmds)
        //     {
        //         using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
        //         using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

        //         // Key name.
        //         var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";

        //         if (_fake)
        //         {
        //             LogInfo($"DeleteSubKeyTree [{ssubkey}]");
        //         }
        //         else
        //         {
        //             regRoot!.DeleteSubKeyTree(ssubkey, false);
        //         }
        //     }
        // }

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
            //MessageBox.Show($"{fspec}{msg}", "Command failed");
        }

        /// <summary>
        /// Utility to look at registry contents of interest.
        /// </summary>
        /// <param name="hive"></param>
        /// <param name="subkey"></param>
        /// <param name="recursive"></param>
        List<string> DumpHive(RegistryHive hive, string subkey, bool recursive = true)
        {
            List<string> res =
            [
                $"",
                $"====================== {hive} {subkey} ======================",
                $""
            ];

            using var hkcr = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(subkey, writable: false);

            List<string> contexts = ["Directory", "DesktopBackground", "Folder", "*"];
            contexts.ForEach(ctx => DoSubkey(regRoot!, ctx));

            void DoSubkey(RegistryKey key, string sname, int indent = 0)
            {
                string sind = new(' ', indent * 4);
                res.Add($"{sind}[{sname}]");

                using var subkey = key.OpenSubKey(sname, writable: false);
                if (subkey is null) return;

                foreach (string sval in subkey.GetValueNames())
                {
                    // "" means default
                    res.Add($"{sind}  [{sval}]:[{subkey.GetValue(sval)}]");
                }

                if (recursive)
                {
                    // Visit the children.
                    var snames = subkey.GetSubKeyNames();
                    snames.ForEach(s => DoSubkey(subkey, s, indent + 1));
                }
            }

            return res;
        }
        #endregion
    }
}
