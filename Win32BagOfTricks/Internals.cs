
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Win32BagOfTricks
{
#pragma warning disable SYSLIB1054, CA1401, CA2101

    public static class Internals
    {
        #region Types

        public const int MOD_ALT = 0x0001;
        public const int MOD_CTRL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;

        public enum ShellEvents : int
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

        #endregion

        #region API


        public static int RegisterShellHook(IntPtr handle)
        {
            int msg = RegisterWindowMessage("SHELLHOOK"); // test for 0?
            RegisterShellHookWindow(handle);
            return msg;
        }

        public static void DeregisterShellHook(IntPtr handle)
        {
            DeregisterShellHookWindow(handle);
        }



        // Rudimentary management of hotkeys. Only supports one (global) handle.
        static List<int> _hotKeyIds = new();

        public static int RegisterHotKey(IntPtr handle, int key, int mod)
        {
            int id = mod ^ key ^ (int)handle;
            RegisterHotKey(handle, id, mod, key);
            _hotKeyIds.Add(id);
            return id;
        }

        public static void UnregisterHotKeys(IntPtr handle)
        {
            _hotKeyIds.ForEach(id => UnregisterHotKey(handle, id));
        }
        

        #endregion


        #region Native methods

        [Flags]
        internal enum MessageBoxFlags : uint
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

        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// ? Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        /// the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        [StructLayout(LayoutKind.Sequential)]
        internal struct ShellExecuteInfo
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



        #region shell32.dll
        /// <summary>Performs an operation on a specified file.
        /// Args: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        /// <summary>Overload of above for nullable args.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, IntPtr lpParameters, IntPtr lpDirectory, int nShow);

        /// <summary>Finer control version of above.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        #endregion


        #region user32.dll
        /// <summary>Rudimentary UI notification from a console application.</summary>
        [DllImport("User32.dll", CharSet = CharSet.Ansi)]
        internal static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);

        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int DeregisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int RegisterShellHookWindow(IntPtr hWnd);

        // Keyboard hooks.
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion


        #endregion
    }
}
