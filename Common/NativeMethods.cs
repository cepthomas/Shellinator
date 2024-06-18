using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;

// C:\Program Files (x86)\Windows Kits\10\Include\10.0.22621.0\um\WinUser.h

// Reference: https://pinvoke.net

// TODO Try CsWin32? or suppress/fix warnings:
// - CA1401 https://stackoverflow.com/a/35819594
// - CA2101 https://stackoverflow.com/a/67127595 
// - SYSLIB1054

namespace Splunk.Common
{
#pragma warning disable SYSLIB1054, CA1401, CA2101

    /// <summary>Interop.</summary>
    public static class NativeMethods
    {
        #region Constants
        public const int MOD_ALT = 0x0001;
        public const int MOD_CTRL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;
        #endregion

        #region Enums
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
        public enum ShowCommands : int
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

        [Flags]
        public enum MessageBoxFlags : uint
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
        #endregion

        #region Structs
        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// ? Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        /// the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        [StructLayout(LayoutKind.Sequential)]
        public struct ShellExecuteInfo
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

        /// <summary>Contains information about a window.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowInfo
        {
            // The size of the structure, in bytes.The caller must set this member to sizeof(WindowInfo).
            public uint cbSize;             
            // The coordinates of the window.
            public Rect rcWindow;           
            // The coordinates of the client area.
            public Rect rcClient;           
            // The window styles.For a table of window styles, see Window Styles.
            public uint dwStyle;            
            // The extended window styles. For a table of extended window styles, see Extended Window Styles.
            public uint dwExStyle;          
            // The window status.If this member is WS_ACTIVECAPTION (0x0001), the window is active.Otherwise, this member is zero.
            public uint dwWindowStatus;     
            // The width of the window border, in pixels.
            public uint cxWindowBorders;    
            // The height of the window border, in pixels.
            public uint cyWindowBorders;    
            // The window class atom (see RegisterClass).
            public ushort atomWindowType;   
            // The Windows version of the application that created the window.
            public ushort wCreatorVersion;  
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion

        #region shell32.dll - Basic shell functions
        /// <summary>Performs an operation on a specified file.
        /// Args: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        /// <summary>Overload of above for nullable args.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, IntPtr lpParameters, IntPtr lpDirectory, int nShow);

        /// <summary>Finer control version of above.</summary>
        [DllImport("shell32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        #endregion

        #region user32.dll - Windows management functions for message handling, timers, menus, and communications
        /// <summary>Rudimentary UI notification from a console application.</summary>
        [DllImport("User32.dll", CharSet = CharSet.Ansi)]
        public static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        #region Window management
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public extern static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Retrieves a handle to the Shell's desktop window.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo winfo);

        /// <summary>Retrieves the thread and process ids that created the window.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr extraData);
        public delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        /// <summary>Copies the text of the specified window's title bar (if it has one) into a buffer.</summary>
        /// <param name="hwnd">handle to the window</param>
        /// <param name="lpString">StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetWindowTextLength(IntPtr hwnd);
        #endregion

        #region Window and Keyboard hooks
        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int DeregisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int RegisterShellHookWindow(IntPtr hWnd);

        // Keyboard hooks.
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
        #endregion
    }
}
