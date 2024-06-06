using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;


// TODO2 https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation


namespace Splunk.Common
{
    /// <summary>Interop.</summary>
    public static class NativeMethods
    {
        #region Constants
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const int ALT = 0x0001;
        public const int CTRL = 0x0002;
        public const int SHIFT = 0x0004;
        public const int WIN = 0x0008;
        public const short SWP_NOMOVE = 0X2;
        public const short SWP_NOSIZE = 0X01;
        public const short SWP_NOZORDER = 0X04;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const uint WM_GETTEXT = 0x000D;
        #endregion

        #region Enums
        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
        public enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
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

        public enum WindowShowState : int
        {
            SW_SHOWNORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
        }

        [Flags]
        public enum AssocF
        {
            Init_NoRemapCLSID = 0x01,
            Init_ByExeName = 0x02,
            Open_ByExeName = 0x02,
            Init_DefaultToStar = 0x04,
            Init_DefaultToFolder = 0x08,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }

        [Flags]
        public enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }
        #endregion

        #region Structs
        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// - TODO2 Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        ///   the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        /// - lpVerb member can be used for a varity of actions like "properties", "find", "openas", "print"..etc depending
        ///   on the file type you're dealing with. Actions available for a specific file type are stored in registry.
        ///   Setting lpVerb to null results in the default action of that file type to be executed.
        [StructLayout(LayoutKind.Sequential)]
        public struct SHELLEXECUTEINFO
        {
            // The size of this structure, in bytes.
            public int cbSize;

            // A combination of one or more ShellExecuteMaskFlags.
            public uint fMask;

            // Optional handle to the owner window.
            public IntPtr hwnd;

            // A string, referred to as a verb, that specifies the action to be performed.
            // open - Opens a file or application.
            // openas - Opens dialog when no program is associated to the extension.
            // runas - Open the Run as... Dialog
            // null - Specifies that the operation is the default for the selected file type.
            // edit - Opens the default text editor for the file.
            // explore - Opens the Windows Explorer in the folder specified in lpDirectory.
            // properties - Opens the properties window of the file.
            // pastelink - pastes a shortcut
            // print - Start printing the file with the default application.
            // find - Start a search
            // Also opennew, copy, cut, paste, delete, printto - see MSDN.
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
        public struct WINDOWINFO
        {
            public uint cbSize;             // The size of the structure, in bytes.The caller must set this member to sizeof(WINDOWINFO).
            public RECT rcWindow;           // The coordinates of the window.
            public RECT rcClient;           // The coordinates of the client area.
            public uint dwStyle;            // The window styles.For a table of window styles, see Window Styles.
            public uint dwExStyle;          // The extended window styles. For a table of extended window styles, see Extended Window Styles.
            public uint dwWindowStatus;     // The window status.If this member is WS_ACTIVECAPTION (0x0001), the window is active.Otherwise, this member is zero.
            public uint cxWindowBorders;    // The width of the window border, in pixels.
            public uint cyWindowBorders;    // The height of the window border, in pixels.
            public ushort atomWindowType;   // The window class atom (see RegisterClass).
            public ushort wCreatorVersion;  // The Windows version of the application that created the window.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;    // x position of upper-left corner
            public int Top;     // y position of upper-left corner
            public int Right;   // x position of lower-right corner
            public int Bottom;  // y position of lower-right corner
            public int Width    { get { return Right - Left; } }
            public int Height   { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        #endregion

        #region shell32.dll
        /// <summary>General shell function execute. See SHELLEXECUTEINFO for programming.</summary>
        /// Example of Property Dialog:
        /// public static void ShowFileProperties(string Filename)
        /// {
        ///     SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
        ///     info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
        ///     info.lpVerb = "properties";
        ///     info.lpFile = Filename;
        ///     info.nShow = SW_SHOW;
        ///     info.fMask = SEE_MASK_INVOKEIDLIST;
        ///     ShellExecuteEx(ref info);
        /// }
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        /// <summary>Performs an operation on a specified file. See ShellExecuteEx() for args.</summary>
        /// Example opens a URL in the default browser:
        /// IntPtr result = ShellExecute(IntPtr.Zero, "open", "http://www.google.com", null, null, SW_NORMAL);
        /// [in, optional] hwnd - A handle to the parent window used for displaying a UI or error messages. This value can be NULL if the operation is not associated with a window.
        /// 
        /// [in, optional] lpOperation - A pointer to a null-terminated string, referred to in this case as a verb, that specifies the action to be performed. The set of available verbs depends on the particular file or folder. Generally, the actions available from an object's shortcut menu /// are available verbs. The following verbs are commonly used:
        ///   edit - Launches an editor and opens the document for editing. If lpFile is not a document file, the function will fail.
        ///   explore - Explores a folder specified by lpFile.
        ///   find - Initiates a search beginning in the directory specified by lpDirectory.
        ///   open - Opens the item specified by the lpFile parameter. The item can be a file or folder.
        ///   print - Prints the file specified by lpFile. If lpFile is not a document file, the function fails.
        ///   runas - Launches an application as Administrator. User Account Control (UAC) will prompt the user for consent to run the application elevated or enter the credentials of an administrator account used to run the application.
        ///   NULL - The default verb is used, if available. If not, the "open" verb is used. If neither verb is available, the system uses the first verb /// listed in the registry.
        /// 
        /// [in] lpFile - A pointer to a null-terminated string that specifies the file or object on which to execute the specified verb. To specify a Shell namespace object, pass the fully qualified parse name. Note that not all verbs are supported on all objects. For example, not all document types support the "print" verb. If a relative path is used for the lpDirectory parameter do not use a relative path for lpFile.
        /// 
        /// [in, optional] lpParameters - If lpFile specifies an executable file, this parameter is a pointer to a null-terminated string that specifies the parameters to be passed to the application. The format of this string is determined by the verb that is to be invoked. If lpFile specifies a document file, lpParameters should be NULL.
        /// 
        /// [in, optional] lpDirectory - A pointer to a null-terminated string that specifies the default (working) directory for the action. If this value is NULL, the current working directory is used. If a relative path is provided at lpFile, do not use a relative path for lpDirectory.
        /// 
        /// [in] nShowCmd - The flags that specify how an application is to be displayed when it is opened. If lpFile
        /// specifies a document file, the flag is simply passed to the associated application. It is up to the application 
        /// to decide how to handle it. It can be any of the values that can be specified in the nCmdShow parameter for 
        /// the ShowWindow function.
        /// 
        /// Return value - If the function succeeds, it returns a value greater than 32. If the function fails, it returns an error value that indicates the cause of the failure. The return value is cast as an HINSTANCE for backward compatibility with 16-bit Windows applications. It is not a true HINSTANCE, however. It can be cast only to an INT_PTR and compared to either 32 or the following error codes below.
        /// 
        /// Return code Description:
        ///   0 - The operating system is out of memory or resources.
        ///   ERROR_FILE_NOT_FOUND - The specified file was not found.
        ///   ERROR_PATH_NOT_FOUND - The specified path was not found.
        ///   ERROR_BAD_FORMAT - The .exe file is invalid (non-Win32 .exe or error in .exe image).
        ///   SE_ERR_ACCESSDENIED - The operating system denied access to the specified file.
        ///   SE_ERR_ASSOCINCOMPLETE - The file name association is incomplete or invalid.
        ///   SE_ERR_DDEBUSY - The DDE transaction could not be completed because other DDE transactions were being processed.
        ///   SE_ERR_DDEFAIL - The DDE transaction failed.
        ///   SE_ERR_DDETIMEOUT - The DDE transaction could not be completed because the request timed out.
        ///   SE_ERR_DLLNOTFOUND - The specified DLL was not found.
        ///   SE_ERR_FNF - The specified file was not found.
        ///   SE_ERR_NOASSOC - There is no application associated with the given file name extension. This error will also be returned if you attempt to print a file that is not printable.
        ///   SE_ERR_OOM - There was not enough memory to complete the operation.
        ///   SE_ERR_PNF - The specified path was not found.
        ///   SE_ERR_SHARE - A sharing violation occurred.
        /// Call GetLastError for extended error information.
        [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        #endregion

        #region user32.dll
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Retrieves a handle to the Shell's desktop window.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO winfo);

        /// <summary>Retrieves the thread and process ids that created the window.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr extraData);
        public delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer.
        /// If the specified window is a control, the text of the control is copied. However, GetWindowText
        /// cannot retrieve the text of a control in another application.
        /// </summary>
        /// <param name="hwnd">handle to the window</param>
        /// <param name="lpString">StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        /// <returns>ErrorCode</returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder lpString, int cch);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetWindowTextLength(IntPtr hwnd);

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

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        public extern static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("User32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, System.Text.StringBuilder lParam);
        #endregion

        #region shlwapi.dll
        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);
        #endregion
    }
}
