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
using Shellinator.Properties;


//TODO2?? service: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service


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

        /// <summary>Log file path.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = false;

        /// <summary>Log detail.</summary>
        readonly bool _logDebug = false;

        /// <summary>Don't use reserved commands.</summary>
        readonly List<string> _reserved = ["edit", "explore", "find", "open", "print", "properties", "runas"];

        /// <summary>New flavor - All the app commands.</summary>
        readonly List<ExplorerCommand> _explorerCommands = [];

        /// <summary>Determine mode.</summary>
        readonly bool _inDev = false;

        /// <summary>Log item level.</summary>
        enum LogLevel { INF, ERR, DBG }
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
                _inDev = Debugger.IsAttached;
                _logDebug = true;
                _exePath = Path.GetDirectoryName(Application.ExecutablePath)!;
                _logPath = Path.Join(_exePath, "shellinator.log");
                FileInfo fi = new(_logPath);
                if (fi.Exists && fi.Length > 50000)
                {
                    var newfn = _logPath.Replace(".log", "_old.log");
                    File.Delete(newfn);
                    File.Move(_logPath, newfn);
                }
                Log(LogLevel.INF, $"==================== Running shellinator ====================");
                Log(LogLevel.DBG, $"Command line [{string.Join("|", appArgs)}]");

                // Set up commands. Config file is assumed to be next to the executable. If missing init with default
                var cfn = Path.Combine(_exePath, "shellinator.ini");
                if (!File.Exists(cfn))
                {
                    Log(LogLevel.INF, $"Copying default.ini");
                    File.WriteAllBytes(cfn, Resources.default_ini);
                }
                _tmit.Snap("Start load config");
                LoadIni(cfn);
                _tmit.Snap("Done load config");

                ///// Process the args: shellinator.exe key context target.
                var key = appArgs.Length > 0 ? appArgs[0].ToLower() : "";
                var context = appArgs.Length > 1 ? appArgs[1].ToLower() : null;
                var target = appArgs.Length > 2 ? appArgs[2] : null;
                var ext = target is null ? "" : Path.GetExtension(target).Replace(".", "");

                Log(LogLevel.DBG, $"key:{key} context:{context} target:{target} ext:{ext}");
                //+++ key:run context:file target:C:\Dev\Apps\Shellinator\bin\net8.0-windows\shellinator.exe ext:exe

                switch (key, context, target)
                {
                    case ("reg", null, null):
                        RegisterAll();
                        break;

                    case ("unreg", null, null):
                        UnregisterAll();
                        break;

                    case ("dev", null, null):
                        Log(LogLevel.INF, $"Shellinator dev mode");
                        _fake = true;
                        RegisterAll();
                        _fake = false;

                        //var dump = DumpHive(RegistryHive.CurrentUser, @"Software\Classes");
                        // DumpHive(RegistryHive.LocalMachine, @"Software\Classes");
                        // DumpHive(RegistryHive.CurrentUser, @"\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");
                        //dump.ForEach(x => Console.WriteLine(x));
                        break;

                    case (_, "dir", not null):
                    case (_, "dirbg", not null):
                    case (_, "deskbg", not null):
                    case (_, "file", not null):
                        // Assume normal mode called from system using registry entry.

                        // var cmds = _explorerCommands.
                        //     Where(c => c.Context == context &&
                        //     c.Key == (context == "file" ? ext : key));

                        ExplorerCommand? cmd = null;

                        // Try standard dirs.
                        var cmds = _explorerCommands.Where(c => c.Context == context && c.Key == key);


                        if (cmds.Any())
                        {
                            cmd = cmds.First();
                        }
                        else
                        {
                            // Try file.
                            cmds = _explorerCommands.Where(c => c.Context == context && c.Key == ext);
                            if (cmds.Any())
                            {
                                cmd = cmds.First();
                            }
                        }


                        if (cmd is not null)
                        {
                            // TODO2 also? Environment.ExpandEnvironmentVariables(expCmd);
                            var replLines = cmd.ExecLine.Select(l => l.Replace("$target", target));
                            _tmit.Snap("Execute command start");
                            var res = ExecuteCommand([.. replLines]);
                            _tmit.Snap("Execute command end");

                            if (res.Code == 0)
                            {
                                // Success. Capture any stdout. TIL don't set clipboard to an empty string.
                                if (res.Stdout.Length > 0) Clipboard.SetText(res.Stdout);
                            }
                            else
                            {
                                // Command failed. Capture everything useful.
                                List<string> ls =
                                [
                                    $"code: {res.Code}",
                                    $"stdout:{Environment.NewLine}{(res.Stdout.Length > 0 ? res.Stdout : "None")}",
                                    $"stderr:{Environment.NewLine}{(res.Stderr .Length > 0 ? res.Stderr : "None")}"
                                ];
                                var sres = string.Join(Environment.NewLine, ls);
                                Log(LogLevel.ERR, sres);
                                Clipboard.SetText(sres);
                                code = 1;
                            }
                        }
                        else
                        {
                            _explorerCommands.ForEach(c => Log(LogLevel.DBG, $"{c}"));
                            throw new ShellinatorException($"Invalid command line");
                        }
                        break;

                    default:
                        throw new ShellinatorException($"Invalid command line args");
                }
            }
            catch (ShellinatorException ex) // app error
            {
                Log(LogLevel.ERR, $"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 2;
            }
            catch (Exception ex) // something else
            {
                Log(LogLevel.ERR, $"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 3;
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => Log(LogLevel.DBG, $"TMIT [{c}]"));

            Environment.Exit(code);
        }
        #endregion

        #region Internals
        /// <summary>
        /// 
        /// </summary>
        void RegisterAll()
        {
            Log(LogLevel.INF, $"Shellinator register all");
            // UnregisterAll(); // clean up first?
            _explorerCommands.ForEach(cmd => { Register(cmd); });
        }

        /// <summary>
        /// 
        /// </summary>
        void UnregisterAll()
        {
            Log(LogLevel.INF, $"Shellinator unregister all");
            _explorerCommands.ForEach(cmd => { Unregister(cmd); });
        }

        /// <summary>
        /// Generic command executor. Called by command handlers. Suppresses console window creation.
        /// </summary>
        /// <param name="args">Command followed by all args.</param>
        /// <returns>Result code, stdout, stderr</returns>
        ExecResult ExecuteCommand(List<string> args)
        {
            Log(LogLevel.DBG, $"ExecuteCommand() [{string.Join("|", args)}]");

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

            // Identify the command and context.
            foreach (var sectName in inrdr.GetSectionNames())
            {
                var sectionParts = sectName.SplitByTokens(" ");
                if (sectionParts.Count < 2) throw new IniSyntaxException($"Invalid section name [{sectName}]", -1);

                var ctxt = sectionParts[0].ToLower();
                var cmd = sectionParts[1].ToLower();
                var sectVals = inrdr.GetValues(sectName);

                if (!sectVals.TryGetValue("menu", out var menu)) throw new IniSyntaxException($"Missing menu item in section [{sectName}]", -1);
                if (!sectVals.TryGetValue("exec", out var exec)) throw new IniSyntaxException($"Missing exec item in section [{sectName}]", -1);
                if (_reserved.Contains(cmd)) { throw new ArgumentException($"Reserved command [{cmd}]"); }

                var execParts = exec.SplitByToken(",");

                // Good to go.
                switch (ctxt)
                {
                    case "dir":
                    case "dirbg":
                    case "deskbg":
                        _explorerCommands.Add(new(ctxt, cmd, menu, execParts));
                        break;

                    case "file":
                        sectionParts[1..].ForEach(ext => _explorerCommands.Add(new(ctxt, ext, menu, execParts)));
                        break;

                    //case "folder":
                    //    break;

                    default:
                        throw new ArgumentException($"Invalid context: {sectionParts[0]}");
                }
            }
        }
        #endregion

        #region Registry editing
        /// <summary>
        /// Write a command to the registry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        void Register(ExplorerCommand cmd)
        {
            // Assemble command: shellinator_path id context target
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var subkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Key}";
            var subkey2 = $"{subkey1}\\command";

            // Determine target tag based on origin. Dir/File/Folder => %D  DirBg/DeskBg => %W.
            var targetTag = cmd.Context.ToLower() switch
            {
                "dirbg" => "%W",
                "deskbg" => "%W",
                _ => "%D",
            };

            // All paths and macros that expand to paths must be wrapped in double quotes.
            var exec = Path.Join(_exePath, "shellinator.exe");
            var expCmd = $"\"{exec}\" {cmd.Key} {cmd.Context} \"{targetTag}\"";

            if (_fake)
            {
                Log(LogLevel.DBG, $"FAKE CreateSubKey({subkey1}) SetValue(MUIVerb, {cmd.Text})");
                Log(LogLevel.DBG, $"FAKE CreateSubKey({subkey2}) SetValue(_, {expCmd})");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(subkey1);
                k1.SetValue("MUIVerb", cmd.Text);

                using var k2 = regRoot!.CreateSubKey(subkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>
        /// Delete existing registry entry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        void Unregister(ExplorerCommand cmd)
        {
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Key}";

            if (_fake)
            {
                Log(LogLevel.DBG, $"FAKE DeleteSubKeyTree({ssubkey})");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey, false);
            }
        }

        /// <summary>
        /// Convert the internal enum to registry key.
        /// </summary>
        static string GetRegPath(string context)
        {
            return context.ToLower() switch
            {
                "dir" => "Directory",
                "dirbg" => "Directory\\Background",
                "deskbg" => "DesktopBackground",
                "file" => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }
        #endregion

        #region Infrastructure
        /// <summary>
        /// Simple logging.
        /// </summary>
        /// <param name="lvl"></param>
        /// <param name="msg"></param>
        /// <param name="line"></param>
        void Log(LogLevel lvl, string msg, [CallerLineNumber] int line = -1)
        {
            string? sind = lvl switch
            {
                LogLevel.INF => "",
                LogLevel.ERR => "!!! ",
                LogLevel.DBG => _logDebug ? ">>> " : null,
                _ => null
            };

            if (sind is not null)
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {sind}({line}) {msg}{Environment.NewLine}");
            }
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
