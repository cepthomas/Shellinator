using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


#pragma warning disable SYSLIB1054, CA1401, CA2101

namespace Win32BagOfTricks
{
    public static class Internals
    {
        #region Definitions
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;

        // Expose some internal definitions.
        public const int MOD_ALT = (int)KeyModifiers.MOD_ALT;
        public const int MOD_CTRL = (int)KeyModifiers.MOD_CTRL;
        public const int MOD_SHIFT = (int)KeyModifiers.MOD_SHIFT;
        public const int MOD_WIN = (int)KeyModifiers.MOD_WIN;
        public const int HSHELL_WINDOWCREATED = (int)ShellEvents.HSHELL_WINDOWCREATED;
        public const int HSHELL_WINDOWDESTROYED = (int)ShellEvents.HSHELL_WINDOWDESTROYED;
        public const int HSHELL_WINDOWACTIVATED = (int)ShellEvents.HSHELL_WINDOWACTIVATED;
        #endregion

        #region API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static int RegisterShellHook(IntPtr handle)
        {
            int msg = RegisterWindowMessage("SHELLHOOK"); // test for 0?
            RegisterShellHookWindow(handle);
            return msg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        public static void DeregisterShellHook(IntPtr handle)
        {
            DeregisterShellHookWindow(handle);
        }

        static List<int> _hotKeyIds = new();
        /// <summary>
        /// Rudimentary management of hotkeys. Only supports one (global) handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int RegisterHotKey(IntPtr handle, int key, int mod)
        {
            int id = mod ^ key ^ (int)handle;
            RegisterHotKey(handle, id, mod, key);
            _hotKeyIds.Add(id);
            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        public static void UnregisterHotKeys(IntPtr handle)
        {
            _hotKeyIds.ForEach(id => UnregisterHotKey(handle, id));
        }

        /// <summary>
        /// Generic limited modal message box. Just enough for a console/hidden application.
        /// </summary>
        /// <param name="message">Body.</param>
        /// <param name="caption">Caption.</param>
        /// <param name="error">Use error icon otherwise info.</param>
        /// <param name="ask">Use OK/cancel buttons.</param>
        /// <returns>True if OK </returns>
        public static bool MessageBox(string message, string caption, bool error = false, bool ask = false)
        {
            bool ok = true;
            uint flags = error ? (uint)MessageBoxFlags.MB_ICONERROR : (uint)MessageBoxFlags.MB_ICONINFORMATION;

            if (ask)
            {
                flags |= (uint)MessageBoxFlags.MB_OKCANCEL;
                int res = MessageBox(IntPtr.Zero, message, caption, flags);
                if (res != 1) //IDOK
                {
                    ok = false;
                }
            }
            else
            {
                MessageBox(IntPtr.Zero, message, caption, flags);
            }

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void DisableDpiScaling()
        {
            SetProcessDPIAware();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verb">Standard verb</param>
        /// <param name="path">Where</param>
        /// <param name="hide">Hide the new window.</param>
        /// <returns>Standard error code</returns>
        public static int ShellExecute(string verb, string path, bool hide = false)
        {
            //If the function succeeds, it returns a value greater than 32. If the function fails, it returns an error value
            //that indicates the cause of the failure. The return value is cast as an HINSTANCE for backward compatibility
            //with 16 - bit Windows applications. It is not a true HINSTANCE, however. It can be cast only to an INT_PTR and
            //compared to either 32 or the following error codes below.

            var ss = new List<string> { "edit", "explore", "find", "open", "print", "properties", "runas" };
            if (ss.Contains(verb)) { throw new ArgumentException($"Invalid verb:{verb}"); }

            int res = (int)ShellExecute(IntPtr.Zero, verb, path, IntPtr.Zero, IntPtr.Zero,
                hide ? (int)ShowCommands.SW_HIDE : (int)ShowCommands.SW_NORMAL);

            return res > 32 ? 0 : res;
        }
        #endregion

        #region Native methods - private

        #region Types
        enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = SW_SHOWNORMAL,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = SW_SHOWMAXIMIZED,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = SW_FORCEMINIMIZE
        }

        [Flags]
        enum MessageBoxFlags : uint
        {
            MB_OK = 0x00000000,
            MB_OKCANCEL = 0x00000001,
            MB_ABORTRETRYIGNORE = 0x00000002,
            MB_YESNOCANCEL = 0x00000003,
            MB_YESNO = 0x00000004,
            MB_RETRYCANCEL = 0x00000005,
            MB_CANCELTRYCONTINUE = 0x00000006,
            MB_ICONHAND = 0x00000010,
            MB_ICONQUESTION = 0x00000020,
            MB_ICONEXCLAMATION = 0x00000030,
            MB_ICONASTERISK = 0x00000040,
            MB_USERICON = 0x00000080,
            MB_ICONWARNING = MB_ICONEXCLAMATION,
            MB_ICONERROR = MB_ICONHAND,
            MB_ICONINFORMATION = MB_ICONASTERISK,
            MB_ICONSTOP = MB_ICONHAND,
            //MB_DEFBUTTON1 = 0x00000000,
            //MB_DEFBUTTON2 = 0x00000100,
            //MB_DEFBUTTON3 = 0x00000200,
            //MB_DEFBUTTON4 = 0x00000300,
            //MB_APPLMODAL = 0x00000000,
            //MB_SYSTEMMODAL = 0x00001000,
            //MB_TASKMODAL = 0x00002000,
            //MB_HELP = 0x00004000, // Help Button
            //MB_NOFOCUS = 0x00008000,
            //MB_SETFOREGROUND = 0x00010000,
            //MB_DEFAULT_DESKTOP_ONLY = 0x00020000,
            //MB_TOPMOST = 0x00040000,
            //MB_RIGHT = 0x00080000,
            //MB_RTLREADING = 0x00100000,
        }

        [Flags]
        enum KeyModifiers : int
        {
            MOD_ALT = 0x0001,
            MOD_CTRL = 0x0002,
            MOD_SHIFT = 0x0004,
            MOD_WIN = 0x0008
        }

        enum ShellEvents : int
        {
            HSHELL_WINDOWCREATED = 1,
            HSHELL_WINDOWDESTROYED = 2,
            HSHELL_ACTIVATESHELLWINDOW = 3, // not used
            HSHELL_WINDOWACTIVATED = 4,
            HSHELL_GETMINRECT = 5,
            HSHELL_REDRAW = 6,
            HSHELL_TASKMAN = 7,
            HSHELL_LANGUAGE = 8,
            HSHELL_ACCESSIBILITYSTATE = 11,
            HSHELL_APPCOMMAND = 12
        }

        [Flags]
        public enum ShellExecuteMaskFlags : uint
        {
            SEE_MASK_DEFAULT = 0x00000000,
            SEE_MASK_CLASSNAME = 0x00000001,
            SEE_MASK_CLASSKEY = 0x00000003,
            SEE_MASK_IDLIST = 0x00000004,
            SEE_MASK_INVOKEIDLIST = 0x0000000c,   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04)
            SEE_MASK_HOTKEY = 0x00000020,
            SEE_MASK_NOCLOSEPROCESS = 0x00000040,
            SEE_MASK_CONNECTNETDRV = 0x00000080,
            SEE_MASK_NOASYNC = 0x00000100,
            SEE_MASK_FLAG_DDEWAIT = SEE_MASK_NOASYNC,
            SEE_MASK_DOENVSUBST = 0x00000200,
            SEE_MASK_FLAG_NO_UI = 0x00000400,
            SEE_MASK_UNICODE = 0x00004000,
            SEE_MASK_NO_CONSOLE = 0x00008000,
            SEE_MASK_ASYNCOK = 0x00100000,
            SEE_MASK_HMONITOR = 0x00200000,
            SEE_MASK_NOZONECHECKS = 0x00800000,
            SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
            SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
            SEE_MASK_FLAG_LOG_USAGE = 0x04000000,
        }

        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// ? Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        /// the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        [StructLayout(LayoutKind.Sequential)]
        struct ShellExecuteInfo
        {
            // The size of this structure, in bytes.
            public int cbSize;
            // A combination of one or more ShellExecuteMaskFlags.
            public uint fMask;
            // Optional handle to the owner window.
            public IntPtr hwnd;
            // Specific operation.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            // null-terminated string that specifies the name of the file or object on which ShellExecuteEx will perform the action specified by the lpVerb parameter.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            // Optional null-terminated string that contains the application parameters separated by spaces.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            // Optional null-terminated string that specifies the name of the working directory.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            // Flags that specify how an application is to be shown when it is opened. See ShowCommands.
            public int nShow;
            // The rest are ?????
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
        #endregion

        #region shell32.dll
        /// <summary>Performs an operation on a specified file.
        /// Args: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        /// <summary>Overload of above for nullable args.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, IntPtr lpParameters, IntPtr lpDirectory, int nShow);

        /// <summary>Finer control version of above.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        #endregion

        #region user32.dll
        /// <summary>Rudimentary UI notification from a console application.</summary>
        [DllImport("User32.dll", CharSet = CharSet.Ansi)]
        static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern int DeregisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern int RegisterShellHookWindow(IntPtr hWnd);

        // Keyboard hooks.
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        #endregion
    }
}
