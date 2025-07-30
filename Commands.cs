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
                new("treex",  ExplorerContext.Dir,    "Treex Dir",              "Copy a tree of selected directory to clipboard",   TreexCmd),
                new("treex",  ExplorerContext.DirBg,  "Treex DirBg",              "Copy a tree here to clipboard.",                   TreexCmd),
                new("openst", ExplorerContext.Dir,    "Open in Sublime Dir",    "Open selected directory in Sublime Text.",         SublimeCmd),
                new("openst", ExplorerContext.DirBg,  "Open in Sublime DirBg",    "Open here in Sublime Text.",                       SublimeCmd),
                new("findev", ExplorerContext.Dir,    "Open in Everything Dir", "Open selected directory in Everything.",           EverythingCmd),
                new("findev", ExplorerContext.DirBg,  "Open in Everything DirBg", "Open here in Everything.",                         EverythingCmd),
                new("exec",   ExplorerContext.File,   "Execute File",            "Execute file if executable otherwise open it.",    ExecCmd),
                //new("test",   ExplorerContext.Dir,    "==Test Dir==",       "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.DirBg,  "==Test DirBg==",     "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.DeskBg, "==Test DeskBg==",    "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.File,   "==Test File==",      "Debug stuff.",                                     TestCmd),
                ///////////new("test",   ExplorerContext.Folder, "==Test Folder==",    "Debug stuff.",                                     TestCmd)
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
                ExplorerContext.Dir => ExecuteCommand([evpath, target]),
                ExplorerContext.DirBg => ExecuteCommand([evpath, target]),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult ExecCmd(ExplorerContext context, string target)
        {
            if (context == ExplorerContext.File)
            {
                var ext = Path.GetExtension(target);

                var res = ext switch
                {
                    ".cmd" or ".bat" => ExecuteCommand([target]),
                    ".ps1" => ExecuteCommand(["powershell", "-executionpolicy", "bypass", "-File", target]),
                    ".lua" => ExecuteCommand(["lua", target]),
                    ".py" => ExecuteCommand(["python", target]),
                    _ => ExecuteCommand([target]) // default just open.
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
