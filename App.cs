using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
//using WI = Ephemera.Win32.Internals;
//using WM = Ephemera.Win32.WindowManagement;
//using CB = Ephemera.Win32.Clipboard;


// TODO can this be a generic tool?  _commands,  Run() cmds,  help??

namespace Shellinator
{
    #region Types
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>See README#Commands. File to support specific extensions?</summary>
    enum ExplorerContext { Dir, DirBg, DeskBg, Folder, File }

    /// <summary>Describes one menu command.</summary>
    /// <param name="Id">Short name for internal id and registry key.</param>
    /// <param name="Context">Explorer context menu origin.</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="CommandLine">Full command string to execute.</param>
    /// <param name="Description">Info about this command.</param>
    readonly record struct ExplorerCommand(string Id, ExplorerContext Context, string Text, string CommandLine, string Description);
    #endregion

    /// <summary>Main app.</summary>
    public class App
    {
        #region Fields
        /// <summary>Measure performance.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        string _shellinatorPath;

        /// <summary>Log file name.</summary>
        readonly string _logFileName;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = true;

        /// <summary>All the commands. Don't use reserved ids: edit, explore, find, open, print, properties, runas</summary>
        readonly List<ExplorerCommand> _commands =
        [
            //new("cmder", ExplorerContext.Dir, "Commander", "%SHELLINATOR %ID \"%D\"", "Open a new explorer next to the current."),
            new("tree", ExplorerContext.Dir, "Tree", "%SHELLINATOR %ID \"%D\"", "Copy a tree of selected directory to clipboard"),
            new("openst", ExplorerContext.Dir, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."),
            new("findev", ExplorerContext.Dir, "Open in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."),
            new("tree", ExplorerContext.DirBg, "Tree", "%SHELLINATOR %ID \"%W\"", "Copy a tree here to clipboard."),
            new("openst", ExplorerContext.DirBg, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."),
            new("findev", ExplorerContext.DirBg, "Open in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%W\"", "Open here in Everything."),
            new("exec", ExplorerContext.File, "Execute", "%SHELLINATOR %ID \"%D\"", "Execute file if executable otherwise opened."),
            //new("test_deskbg", ExplorerContext.DeskBg, "!! Test DeskBg", "%SHELLINATOR %ID \"%W\"", "Debug stuff."),
            //new("test_folder", ExplorerContext.Folder, "!! Test Folder", "%SHELLINATOR %ID \"%D\"", "Debug stuff."),
        ];
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            string appDir = MiscUtils.GetAppDataDir("Shellinator", "Ephemera");

            _logFileName = Path.Join(appDir, "Shellinator.txt");

            // Gets the icon associated with the currently executing assembly.
            //Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            Log($"Shellinator command args:{string.Join(" ", args)}");

            _tmit.Snap("Here we go!");

            // Execute. Run throws various exceptions depending on the origin of the error.
            try
            {
                Environment.ExitCode = Run([.. args]);
            }
            catch (ShellinatorException ex)
            {
                Environment.ExitCode = 100;
                Log(ex.Message, true);
            }
            catch (Exception ex) // something else
            {
                Environment.ExitCode = 200;
                Log(ex.Message, true);
            }

            if (Environment.ExitCode != 0)
            {
                // TODO notifications WI.MessageBox("Shellinator error", "See the log", true);
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => Log(c));

            //Log($"Exit code:{Environment.ExitCode} msec:{sw.ElapsedMilliseconds}");

            // Before we end, manage log file.
            FileInfo fi = new(_logFileName);
            if (fi.Exists && fi.Length > 10000)
            {
                var lines = File.ReadAllLines(_logFileName);
                int start = lines.Length / 3;
                var trunc = lines.Subset(start, lines.Length - start);
                File.WriteAllLines(_logFileName, trunc);
                Log($"============================ Trimmed log file ============================");
            }
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <return>Execute return code.</return>
        public int Run(List<string> args)
        {
            // Process the args => Shellinator.exe id path
            if (args.Count != 2)
            {
                throw new ShellinatorException($"Invalid command line format");
            }

            var id = Environment.ExpandEnvironmentVariables(args[0]);
            var path = Environment.ExpandEnvironmentVariables(args[1]);

            // Check for valid path.
            if (path.StartsWith("::"))
            {
                throw new ShellinatorException($"Can't use magic system folders e.g. Home");
            }
            else if (!Path.Exists(path))
            {
                throw new ShellinatorException($"Invalid path [{path}]");
            }

            // Final details.
            FileAttributes attr = File.GetAttributes(path);
            var wdir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;
            var isdir = attr.HasFlag(FileAttributes.Directory);

            Log($"Run() id:{id} path:{path} wdir:{wdir} isdir:{isdir}");

            (int code, string stdout, string stderr) ret = new();

            switch (id)
            {
                case "tree":
                {
                    ret = ExecuteCommand("cmd", wdir, $"/c tree /a /f \"{wdir}\"");
                }
                break;

                case "exec":
                {
                    if (!isdir)
                    {
                        var ext = Path.GetExtension(path);
                        ret = ext switch
                        {
                            ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c \"{path}\""),
                            ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{path}\""),
                            ".lua" => ExecuteCommand("lua", wdir, $"\"{path}\""),
                            ".py" => ExecuteCommand("python", wdir, $"\"{path}\""),
                            _ => ExecuteCommand("cmd", wdir, $"/c \"{path}\"") // default just open.
                        };
                    }
                    // else ignore selection of dir
                }
                break;

                case "_config":

                    // Internal management commands.

                    // write
                    _commands.ForEach(CreateRegistryEntry);

                    // delete
                    _commands.ForEach(RemoveRegistryEntry);

                    break;

                //case "test_deskbg":
                //case "test_folder":
                //{
                //    Log($"!!! Got {id}:{path}");
                //    WI.MessageBox($"!!! Got {id}:{path}", "Debug");
                //}
                //break;

                default:
                    throw new ShellinatorException($"Invalid id:{id}");
            }

            if (ret.code != 0)
            {
                Log($"Run() retCode:[{ret.code}]");
                if (ret.stdout != "")
                {
                    Log($"stdout: [{ret.stdout}]");
                }
                if (ret.stderr != "")
                {
                    Log($"stderr: [{ret.stderr}]");
                }
            }

            return ret.code;
        }

        /// <summary>
        /// Generic command executor with hidden console.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="wdir"></param>
        /// <param name="args"></param>
        /// <returns>(code, stdout, stderr)</returns>
        (int code, string stdout, string stderr) ExecuteCommand(string exe, string wdir, string args)
        {
            Log($"ExecuteCommand() exe:[{exe}] wdir:[{wdir}] args:[{args}]");

            ProcessStartInfo pinfo = new()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = wdir, // needed?
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using Process proc = new() { StartInfo = pinfo };
            proc.Start();
            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return (proc.ExitCode, stdout, stderr);
        }

        /// <summary>Write command to the registry.</summary>
        /// <param name="ecmd">Which command</param>
        void CreateRegistryEntry(ExplorerCommand ecmd)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{GetRegPath(ecmd.Context)}\\shell\\{ecmd.Id}";
            var ssubkey2 = $"{ssubkey1}\\command";
            var expCmd = ecmd.CommandLine.Replace("%SHELLINATOR", $"\"{_shellinatorPath}\"").Replace("%ID", ecmd.Id);
            expCmd = Environment.ExpandEnvironmentVariables(expCmd);

            if (_fake)
            {
                Debug.WriteLine($"Create [{ssubkey1}]  MUIVerb={ecmd.Text}");
                Debug.WriteLine($"Create [{ssubkey2}]  @={expCmd}");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(ssubkey1);
                k1.SetValue("MUIVerb", ecmd.Text);

                using var k2 = regRoot!.CreateSubKey(ssubkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>Delete registry entry.</summary>
        void RemoveRegistryEntry(ExplorerCommand ecmd)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(ecmd.Context)}\\shell\\{ecmd.Id}";

            if (_fake)
            {
                Debug.WriteLine($"Delete [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey);
            }
        }

        /// <summary>Convert the shell context to registry key.</summary>
        string GetRegPath(ExplorerContext context)
        {
            return context switch
            {
                ExplorerContext.Dir => "Directory",
                ExplorerContext.DirBg => "Directory\\Background",
                ExplorerContext.DeskBg => "DesktopBackground",
                ExplorerContext.Folder => "Folder",
                ExplorerContext.File => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }

        /// <summary>Simple logging, don't need a full-blown logger.</summary>
        void Log(string msg, bool error = false)
        {
            string cat = error ? " ERR " : " ";
            File.AppendAllText(_logFileName, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff}{cat}{msg}{Environment.NewLine}");
        }
    }
}
