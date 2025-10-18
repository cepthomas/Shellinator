using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace Shellinator
{
    /// <summary>The customized part of the app.</summary>
    internal partial class App
    {
        /// <summary>Set up commands.</summary>
        void InitCommands()
        {
            _commands =
            [
                new("treex",  ExplorerContext.Dir,    "Treex",              "Copy a tree of selected directory to clipboard",   TreexCmd),
                new("treex",  ExplorerContext.DirBg,  "Treex",              "Copy a tree here to clipboard.",                   TreexCmd),
                new("openst", ExplorerContext.Dir,    "Open in Sublime",    "Open selected directory in Sublime Text.",         SublimeCmd),
                new("openst", ExplorerContext.DirBg,  "Open in Sublime",    "Open here in Sublime Text.",                       SublimeCmd),
                new("findev", ExplorerContext.Dir,    "Open in Everything", "Open selected directory in Everything.",           EverythingCmd),
                new("findev", ExplorerContext.DirBg,  "Open in Everything", "Open here in Everything.",                         EverythingCmd),
                new("run",    ExplorerContext.File,   "Run",                "Execute or open the file.",                        RunCmd),
            ];
        }

        //--------------------------------------------------------//
        ExecResult TreexCmd(ExplorerContext context, string target)
        {
            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand(["treex", "-c", target]),
                ExplorerContext.DirBg => ExecuteCommand(["treex", "-c", target]),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult SublimeCmd(ExplorerContext context, string target)
        {
            var stpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Sublime Text", "subl");

            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand([stpath, "--launch-or-new-window", target]),
                ExplorerContext.DirBg => ExecuteCommand([stpath, "--launch-or-new-window", target]),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult EverythingCmd(ExplorerContext context, string target)
        {
            var evpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Everything", "everything");

            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand([evpath, "-parent", target]),
                ExplorerContext.DirBg => ExecuteCommand([evpath, "-parent", target]),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult RunCmd(ExplorerContext context, string target)
        {
            if (context == ExplorerContext.File)
            {
                var ext = Path.GetExtension(target);

                var res = ext switch
                {
                    ".cmd" or ".bat" => ExecuteCommand([target], true),
                    ".ps1" => ExecuteCommand(["powershell", "-executionpolicy", "bypass", "-File", target]),
                    ".lua" => ExecuteCommand(["lua", target]),
                    ".py" => ExecuteCommand(["py", target]),
                    _ => ExecuteCommand([target], true) // default just open.
                };

                return res;
            }
            else
            {
                throw new ShellinatorException($"Invalid context: {context}");
            }
        }

        //--------------------------------------------------------//
        ExecResult TestCmd(ExplorerContext context, string target)
        {
            LogInfo($"!!! Test context:[{context}] target:[{target}]");
            return new();
        }
    }
}
