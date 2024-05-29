using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;


// shell.dll contains the basics:
// --------------------------------
// ShellExecute  ShellExecuteEx  ShellExecuteExW
// SHGetInstanceExplorer
// 
// DllGetVersion  DLLGETVERSIONINFO  ExtendedFileInfo  ExtractAssociatedIcon  ExtractIcon  ExtractIconEx  FileIconInit
// EnumFontFamExProc  FindExecutable  IShellIcon  IsNetDrive  ITaskbarList  ITaskbarList2  ITaskbarList3  ITaskbarList4
// KNOWNFOLDERID  SHAddToRecentDocs  SHAppBarMessage  SHBrowseForFolder  ShellAbout
// ShellExecute  ShellExecuteEx  ShellExecuteExW
// Shell_GetImageLists  Shell_NotifyIcon  Shell_NotifyIconGetRect  SHGetDesktopFolder
// SHGetFolderLocation  SHGetFolderPath  SHGetKnownFolderPath  SHGetImageList  SHGetSpecialFolderLocation
// SHGetSpecialFolderPath  SHGetSpecialFolderPathA  SHGetStockIconInfo  SHSetKnownFolderPath
// 
// api:   APPBARDATA   APPBARDATA   BatchExec   CharSet   CommandLineToArgvW   CSIDL   CSIDL   dll ILCLONEFULL   DoEnvironmentSubst   
// DragAcceptFiles   DragFinish   DragQueryFile   DragQueryPoint   DuplicateIcon   ERazMA   FileSystemWatcher   FZ79pQ   
// GetFinalPathNameByHandle   HChangeNotifyEventID   HChangeNotifyFlags   ILClone   ILCombine   ILCreateFromPath   ILFindLastID   
// ILFree   ILIsEqual   ILIsParent   ILRemoveLastID   IsUserAnAdmin   ljlsjsf   PathCleanupSpec   PathIsExe   PathMakeUniqueName   
// PathYetAnotherMakeUniqueName   PickIconDlg   Run   SetCurrentProcessExplicitAppUserModelID   SHBindToParent   SHChangeNotify   
// SHChangeNotifyRegister   SHChangeNotifyUnregister   SHCNRF   SHCreateDirectoryEx   SHCreateItemFromIDList   // SHCreateItemFromParsingName
// SHCreateItemWithParent   SHCreateProcessAsUserW   SHEmptyRecycleBin   SHFileOperation   SHFormatDrive   SHFreeNameMappings   
// SHGetDataFromIDList   SHGetDiskFreeSpace   SHGetFileInfo   SHGetFileInfoA   SHGetIconOverlayIndex   SHGetIDListFromObject   
// SHGetInstanceExplorer   SHGetMalloc   SHGetNameFromIDList   SHGetNewLinkInfo   SHGetPathFromIDList   
// SHGetPropertyStoreFromParsingNamehtml   SHGetRealIDL   SHGetSetSettings   SHGetSettings   SHInvokePrinterCommand   
// SHIsFileAvailableOffline   SHLoadInProc   SHLoadNonloadedIconOverlayIdentifiers   SHObjectProperties   SHOpenFolderAndSelectItems   
// SHOpenWithDialog   SHParseDisplayName   SHParseDisplayName   SHPathPrepareForWrite   SHQueryRecycleBin
// SHQueryUserNotificationState   SHRunFileDialog   SHSetUnreadMailCount   StartInfo   THUMBBUTTON   ultimate   virt girl hd
// 
// 
// shlwapi.dll contains
// -----------------------
// a collection of functions that provide support for various shell operations, such as 
// file and folder manipulation, user interface elements, and internet-related tasks.
// 
// AssocCreate  AssocGetPerceivedType  AssocQueryString  ColorHLSToRGB  ColorRGBToHLS  HashData  IPreviewHandler  IsOS
// PathAddBackslash  PathAppend  PathBuildRoot  PathCanonicalize  PathCombine  PathCommonPrefix  PathCompactPath
// PathCompactPathEx  PathCreateFromUrl  PathFileExists  PathFindNextComponent  PathFindOnPath  PathGetArgs
// PathIsDirectory  PathIsFileSpec  PathIsHTMLFile  PathIsNetworkPath  PathIsRelative  PathIsRoot  PathIsSameRoot
// PathIsUNC  PathIsUNCServer  PathIsUNCServerShare  PathIsURL  PathMatchSpec  PathQuoteSpaces  PathRelativePathTo
// PathRemoveArgs  PathRemoveBackslash  PathRemoveBlanks  PathRemoveExtension  PathRemoveFileSpec  PathRenameExtension
// PathStripPath  PathStripToRoot  PathUndecorate  PathUnExpandEnvStrings  PathUnQuoteSpaces  SHAutoComplete
// SHCreateStreamOnFile  SHCreateStreamOnFileEx  SHLoadIndirectString  SHMessageBoxCheck  StrCmpLogicalW
// StrFormatByteSize  StrFormatByteSizeA  StrFromTimeInterval  UrlCreateFromPath



namespace Splunk.Common
{
    /// <summary>Interop.</summary>
    public static class NativeMethods
    {
        // TODO2 https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation

        #region Interop constants
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

        #region Types
        /// <summary>
        /// Contains window information.
        /// </summary>
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
            // public static WINDOWINFO GetNewWindoInfo()
            // {
            //     WINDOWINFO result = new WINDOWINFO();
            //     result.cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            //     return result;
            // }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
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


        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        // Retrieves a handle to the Shell's desktop window.
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO winfo);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally,
        /// the identifier of the process that created the window. 
        /// </summary>
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

        public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumWindows(EnumThreadWindowsCallback callback, IntPtr extraData);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer.
        /// If the specified window is a control, the text of the control is copied. However, GetWindowText
        /// cannot retrieve the text of a control in another application.
        /// </summary>
        /// <param name="hwnd">An IntPtr handle to the window</param>
        /// <param name="lpString">A StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        /// <returns>ErrorCode</returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder lpString, int cch);

        // Window hooks.
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

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra,
            [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("User32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        ///// <summary>
        ///// Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window. 
        ///// </summary>
        ///// <param name="hWnd">Window handle</param>
        ///// <param name="wCmd">The relationship between the specified window and the window whose handle is to be retrieved:
        ///// GW_CHILD (5): The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window; otherwise, the retrieved handle is NULL.The function examines only child windows of the specified window.It does not examine descendant windows.
        ///// GW_ENABLEDPOPUP (6): The retrieved handle identifies the enabled popup window owned by the specified window(the search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window.
        ///// GW_HWNDFIRST (0): The retrieved handle identifies the window of the same type that is highest in the Z order.If the specified window is a topmost window, the handle identifies a topmost window.If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        ///// GW_HWNDLAST (1): The retrieved handle identifies the window of the same type that is lowest in the Z order.If the specified window is a topmost window, the handle identifies a topmost window.If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        ///// GW_HWNDNEXT (2): The retrieved handle identifies the window below the specified window in the Z order.If the specified window is a topmost window, the handle identifies a topmost window.If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        ///// GW_HWNDPREV (3): The retrieved handle identifies the window above the specified window in the Z order.If the specified window is a topmost window, the handle identifies a topmost window.If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
        ///// GW_OWNER (4): The retrieved handle identifies the specified window's owner window, if any. For more information, see Owned Windows.
        ///// </param>
        ///// <returns></returns>
        //[DllImport("user32.dll", SetLastError = true)]
        //public static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, System.Text.StringBuilder lParam);

        //[DllImport("user32.dll", EntryPoint = "GetWindowTextLengthA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        //public static extern int GetWindowTextLength(IntPtr hwnd);

        //[DllImport("user32.dll")]
        //static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWindowsCallback lpfn, IntPtr lParam);

        //[DllImport("user32.dll")]
        //private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        //[DllImport("user32.dll")]
        //private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //private int key;
        //private IntPtr hWnd;
        //private int id;
        //private int modifier;

        //public KeyHandler(Form form, Keys key, int modifier = 0)
        //{
        //    this.key = (int)key;
        //    this.hWnd = form.Handle;
        //    this.modifier = modifier;
        //    id = this.GetHashCode();
        //}

        //public override int GetHashCode()
        //{
        //    return modifier ^ key ^ hWnd.ToInt32();
        //}

        //public bool Register()
        //{
        //    return RegisterHotKey(hWnd, id, modifier, key);
        //}

        //public bool Unregiser()
        //{
        //    return UnregisterHotKey(hWnd, id);
        //}
    }
}
