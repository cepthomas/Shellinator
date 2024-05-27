using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;
using NM = Splunk.Common.NativeMethods;
//using static Splunk.Common.NativeMethods;


namespace Splunk.Common
{

    // - Explorer
    // keyboard shortcuts (incl explorer):
    // https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec

    // - powershell
    // https://learn.microsoft.com/en-us/powershell/?view=powershell-7.2
    // https://ss64.com/ps/powershell.html
    // open a new explorer instance:
    // powershell.exe -command Invoke-Item C:\Temp -> not
    // Easy enough from PowerShell using the shell.application com object. 
    //
    // HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Directory\background\shell\Powershell\command:
    // powershell.exe -noexit -command Set-Location -literalPath '%V'



    //https://www.geoffchappell.com/studies/windows/shell/explorer/index.htm?tx=28
    //A typical occasion is when a shell namespace object is to be opened by calling the
    //SHELL32 function ShellExecuteEx and the database of file associations names EXPLORER.EXE
    //as the program to run for applying the requested verb to the object. 
    //
    // A typical occasion is when a shell namespace object is to be opened by calling the SHELL32 function
    // ShellExecuteEx and the database of file associations names EXPLORER.EXE as the program to run for applying
    // the requested verb to the object. 
    //
    // The Windows Explorer Command Line
    // The EXPLORER command line is a sequence of fields with commas and equals signs serving as separators. To allow commas
    // and equals signs within a field, there is a facility for enclosure by double-quotes. The double-quotes are otherwise
    // ignored, except that two consecutive double-quotes in the command line pass into the extracted field as one
    // literal double-quote. White space is ignored at the start and end of each field.

    // Each argument for EXPLORER is one or more fields, shown below as if separated only by commas and without the complications
    // of white space or quoting. Where the first field is a command-line switch, necessarily beginning with the forward
    // slash, it is case-insensitive.
    //
    // /e   
    // /idlist,:handle:process
    //     specifies object as ITEMIDLIST in shared memory block with given handle in context of given process
    //
    // /n
    //     redundant in Windows Vista
    // /root,/idlist,:handle:process
    // /root,clsid
    // /root,clsid,path
    // /root,path
    //     specifies object as root
    //
    // /select
    //     show object as selected item in parent folder
    //
    // /separate
    //     show in separate EXPLORER process
    //
    // path
    //     specifies object;
    //     ignored if object already specified;
    //     overridden by specification in later /idlist or /root argument
    //
    // The overall aim of the command line is to specify a shell namespace object and a way in which EXPLORER is to show that object.

    // public class ExplorerStuff
    // {
    // public void Do()
    // {
    // int l = (int)MouseButtons.Left; // 0x00100000
    // int m = (int)MouseButtons.Middle; // 0x00400000
    // int r = (int)MouseButtons.Right; // 0x00200000

    // // (explorer middle button?) ctrl-T opens selected in new tab
    // }

    // public void DoThisMaybe()
    // {
    // // In my code, I am using VirtualKeyCode.VK_4 because the File Explorer shortcut is the fourth item after
    // // the standard Windows icons. Change the VK_4 value to match the position of File Explorer on your taskbar
    // // (note: File Explorer needs to be pinned to the taskbar, otherwise it won't work).
    // // On my notebook, a delay of at least 250 milliseconds is required (sim.Keyboard.Sleep(250);) before
    // // simulating CTRL+T to add a new tab, change this value according to the power of your processor.
    // // The application is of the Console type, using C# .NET 6, with the InputSimulator NuGet to detect
    // // the pressed keys

    // //Simulate LWIN+4
    // //var sim = new InputSimulator();
    // //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_4);
    // //sim.Keyboard.Sleep(250);
    // ////Simulate CTRL+T
    // //sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);
    // }


    //The Shell allows you to enumerate all open Shell Views. It is more efficient and more robust than
    //checking the process name of every process in the system:
    //https://stackoverflow.com/questions/1190423/using-setwindowpos-in-c-sharp-to-move-windows-around



    /*

    https://learn.microsoft.com/en-us/windows/win32/shell/launch

    shell.dll contains the basics:
    --------------------------------
    ShellExecute
    ShellExecuteEx
    ShellExecuteExW

    SHGetInstanceExplorer

    DllGetVersion  DLLGETVERSIONINFO  ExtendedFileInfo  ExtractAssociatedIcon  ExtractIcon  ExtractIconEx  FileIconInit
    EnumFontFamExProc  FindExecutable  IShellIcon  IsNetDrive  ITaskbarList  ITaskbarList2  ITaskbarList3  ITaskbarList4
    KNOWNFOLDERID  SHAddToRecentDocs  SHAppBarMessage  SHBrowseForFolder  ShellAbout
    ShellExecute  ShellExecuteEx  ShellExecuteExW
    Shell_GetImageLists  Shell_NotifyIcon  Shell_NotifyIconGetRect  SHGetDesktopFolder
    SHGetFolderLocation  SHGetFolderPath  SHGetKnownFolderPath  SHGetImageList  SHGetSpecialFolderLocation
    SHGetSpecialFolderPath  SHGetSpecialFolderPathA  SHGetStockIconInfo  SHSetKnownFolderPath

    api   APPBARDATA   APPBARDATA   BatchExec   CharSet   CommandLineToArgvW   CSIDL   CSIDL   dll ILCLONEFULL   DoEnvironmentSubst   
    DragAcceptFiles   DragFinish   DragQueryFile   DragQueryPoint   DuplicateIcon   ERazMA   FileSystemWatcher   FZ79pQ   
    GetFinalPathNameByHandle   HChangeNotifyEventID   HChangeNotifyFlags   ILClone   ILCombine   ILCreateFromPath   ILFindLastID   
    ILFree   ILIsEqual   ILIsParent   ILRemoveLastID   IsUserAnAdmin   ljlsjsf   PathCleanupSpec   PathIsExe   PathMakeUniqueName   
    PathYetAnotherMakeUniqueName   PickIconDlg   Run   SetCurrentProcessExplicitAppUserModelID   SHBindToParent   SHChangeNotify   
    SHChangeNotifyRegister   SHChangeNotifyUnregister   SHCNRF   SHCreateDirectoryEx   SHCreateItemFromIDList   SHCreateItemFromParsingName
    SHCreateItemWithParent   SHCreateProcessAsUserW   SHEmptyRecycleBin   SHFileOperation   SHFormatDrive   SHFreeNameMappings   
    SHGetDataFromIDList   SHGetDiskFreeSpace   SHGetFileInfo   SHGetFileInfoA   SHGetIconOverlayIndex   SHGetIDListFromObject   
    SHGetInstanceExplorer   SHGetMalloc   SHGetNameFromIDList   SHGetNewLinkInfo   SHGetPathFromIDList   
    SHGetPropertyStoreFromParsingNamehtml   SHGetRealIDL   SHGetSetSettings   SHGetSettings   SHInvokePrinterCommand   
    SHIsFileAvailableOffline   SHLoadInProc   SHLoadNonloadedIconOverlayIdentifiers   SHObjectProperties   SHOpenFolderAndSelectItems   
    SHOpenWithDialog   SHParseDisplayName   SHParseDisplayName   SHPathPrepareForWrite   SHQueryRecycleBin
    SHQueryUserNotificationState   SHRunFileDialog   SHSetUnreadMailCount   StartInfo   THUMBBUTTON   ultimate   virt girl hd


    shlwapi.dll contains
    -----------------------
    a collection of functions that provide support for various shell operations, such as 
    file and folder manipulation, user interface elements, and internet-related tasks.

    AssocCreate  AssocGetPerceivedType  AssocQueryString  ColorHLSToRGB  ColorRGBToHLS  HashData  IPreviewHandler  IsOS
    PathAddBackslash  PathAppend  PathBuildRoot  PathCanonicalize  PathCombine  PathCommonPrefix  PathCompactPath
    PathCompactPathEx  PathCreateFromUrl  PathFileExists  PathFindNextComponent  PathFindOnPath  PathGetArgs
    PathIsDirectory  PathIsFileSpec  PathIsHTMLFile  PathIsNetworkPath  PathIsRelative  PathIsRoot  PathIsSameRoot
    PathIsUNC  PathIsUNCServer  PathIsUNCServerShare  PathIsURL  PathMatchSpec  PathQuoteSpaces  PathRelativePathTo
    PathRemoveArgs  PathRemoveBackslash  PathRemoveBlanks  PathRemoveExtension  PathRemoveFileSpec  PathRenameExtension
    PathStripPath  PathStripToRoot  PathUndecorate  PathUnExpandEnvStrings  PathUnQuoteSpaces  SHAutoComplete
    SHCreateStreamOnFile  SHCreateStreamOnFileEx  SHLoadIndirectString  SHMessageBoxCheck  StrCmpLogicalW
    StrFormatByteSize  StrFormatByteSizeA  StrFromTimeInterval  UrlCreateFromPath
    */




    public class ShellUtils
    {
        // const int VIS_WINDOWS_KEY = (int)Keys.W;

        // const int ALL_WINDOWS_KEY = (int)Keys.A;

        // readonly Stopwatch _sw = new();

        // readonly int _shellHookMsg;

        // #region Events
        // public event Action<IntPtr> WindowCreatedEvent;

        // public event Action<IntPtr> WindowActivatedEvent;

        // public event Action<IntPtr> WindowDestroyedEvent;

        // public event Action KeypressArrangeVisibleEvent;

        // public event Action KeypressArrangeAllEvent;

        // //whf.KeypressArrangeVisibleEvent += Whf_KeypressArrangeVisibleEvent;
        // //whf.KeypressArrangeAllEvent += Whf_KeypressArrangeAllEvent;
        // #endregion


        static void Log(string msg)
        {
            Console.WriteLine(msg);
            //tvInfo.AppendLine("ERROR 100"); TODO1
        }

        public static string ExecInNewProcess2()
        {
            string ret = "Nada";

            Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("echo !!Oscar");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit(); // wait for the process to complete before continuing and process.ExitCode
            ret = cmd.StandardOutput.ReadToEnd();

            return ret;
        }

        public static void MoveWindow(IntPtr handle)
        {
            //var form = FromHandle(handle); TODO1 use WindowInfo[]

            //if (form != null)
            //{
            //    // TODO1 Move it.
            //    //If you add SWP_NOSIZE, it will not resize the window, but will still reposition it.
            //    bool b = SetWindowPos(handle, 0, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
            //    b = SetWindowPos(handle, 0, 0, 0, form.Bounds.Width, form.Bounds.Height, SWP_NOZORDER | SWP_SHOWWINDOW);
            //    //public extern static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);
            //}
            //else
            //{
            //    Log("ERROR 100");
            //}
        }





        public static void GetExplorerWindows()
        {
            // Properties may open new explorers in same process or separate processes.

            ///// Get all explorer processes.
            Process[] procs = Process.GetProcessesByName("explorer");
            var procids = procs.Select(p => p.Id).ToList();
            foreach (var p in procs)
            {
                var handle = p.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    var wi = GetWindowInfo(handle);
                    if (procids.Contains(wi.Pid))
                    {
                        Log($"XPL:{wi}");
                    }
                    else
                    {
                        Log("ERROR ??? 200");
                    }
                }
                else
                {
                    Log("ERROR ??? 300");
                }
            }
            // One entry per seperate process (I think):
            //XPL: Title[] Geometry[X:0 Y: 1020 W: 1920 H: 60] IsVisible[True] Handle[131326] Pid[5748]


            ///// Get all windows in explorer procs, not just the main window.
            // (https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows?redirectedfrom=MSDN)
            List<IntPtr> visibleWindowHandles = [];
            bool addWindowHandle(IntPtr hWnd, IntPtr param)
            {
                if (NM.IsWindowVisible(hWnd))
                {
                    visibleWindowHandles.Add(hWnd);
                }
                return true;
            }
            IntPtr param = IntPtr.Zero;
            NM.EnumWindows(addWindowHandle, param);

            foreach (var vh in visibleWindowHandles)
            {
                var wi = GetWindowInfo(vh);
                if (procids.Contains(wi.Pid))
                {
                    Log($"VIS:{wi}");
                }
                else
                {

                }
            }
            // No explorers
            //VIS: Title[Program Manager] Geometry[X: 0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65872] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 1020 W: 1920 H: 60] IsVisible[True] Handle[131326] Pid[5748]
            //VIS: Title[] Geometry[X:19 Y: 0 W: 1901 H: 4] IsVisible[True] Handle[65980] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65996] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65930] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65928] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65924] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65922] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65948] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[985800] Pid[5748]
            //
            // Two explorers, 1 tab, 2 tab
            //VIS: Title[Program Manager] Geometry[X: 0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65872] Pid[5748]
            //VIS: Title[C: \Users\cepth\OneDrive\OneDriveDocuments] Geometry[X: 501 Y: 0 W: 1258 H: 923] IsVisible[True] Handle[265196] Pid[5748]
            //VIS: Title[C:\Dev] Geometry[X: 469 Y: 94 W: 1258 H: 923] IsVisible[True] Handle[589906] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 1020 W: 1920 H: 60] IsVisible[True] Handle[131326] Pid[5748]
            //VIS: Title[] Geometry[X:19 Y: 0 W: 1901 H: 4] IsVisible[True] Handle[65980] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65996] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65930] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65928] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65924] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 0 H: 0] IsVisible[True] Handle[65922] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65948] Pid[5748]
            //VIS: Title[] Geometry[X:0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[985800] Pid[5748]
        }

        public static WindowInfo GetWindowInfo(IntPtr handle)
        {
            IntPtr threadId = NM.GetWindowThreadProcessId(handle, out IntPtr pid);
            NM.GetWindowRect(handle, out NM.RECT rect);

            StringBuilder sb = new(1024);
            int tlen = NM.GetWindowText(handle, sb, sb.Capacity);

            NM.WINDOWINFO wininfo = new();
            NativeMethods.GetWindowInfo(handle, ref wininfo);

            WindowInfo wi = new()
            {
                Handle = handle,
                ThreadId = threadId,
                Pid = pid.ToInt32(),
                Parent = NM.GetParent(handle),
                NativeInfo = wininfo,
                Title = sb.ToString(),
                IsVisible = NM.IsWindowVisible(handle)
            };

            return wi;
        }



        /*
                public string GetFileAssociationInfo(NativeMethods.AssocStr assocStr, string ext, string? verb = null)
                {
                    uint pcchOut = 0;
                    _ = NativeMethods.AssocQueryString(NativeMethods.AssocF.Verify, assocStr, ext, verb, null, ref pcchOut);
                    StringBuilder pszOut = new((int)pcchOut);
                    _ = NativeMethods.AssocQueryString(NativeMethods.AssocF.Verify, assocStr, ext, verb, pszOut, ref pcchOut);
                    return pszOut.ToString();
                }

                // Fancier one.
                public string GetFileAssociationInfo2(string ext, string? verb = null)
                {
                    if (ext[0] != '.')
                    {
                        ext = "." + ext;
                    }

                    string executablePath = GetFileAssociationInfo(NativeMethods.AssocStr.Executable, ext, verb); // Will only work for 'open' verb

                    if (string.IsNullOrEmpty(executablePath))
                    {
                        executablePath = GetFileAssociationInfo(NativeMethods.AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

                        // Extract only the path
                        if (!string.IsNullOrEmpty(executablePath) && executablePath.Length > 1)
                        {
                            if (executablePath[0] == '"')
                            {
                                executablePath = executablePath.Split('\"')[1];
                            }
                            else if (executablePath[0] == '\'')
                            {
                                executablePath = executablePath.Split('\'')[1];
                            }
                        }
                    }

                    // Ensure to not return the default OpenWith.exe associated executable in Windows 8 or higher
                    if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath) && !executablePath.ToLower().EndsWith(".dll"))
                    {
                        // 'OpenWith.exe' is the windows 8 or higher default for unknown extensions.
                        // I don't want to have it as associted file
                        if (executablePath.ToLower().EndsWith("openwith.exe"))
                        {
                            return null; 
                        }
                        return executablePath;
                    }
                    return executablePath;
                }





        public Icon GetSmallFolderIcon()
        {
            return GetIcon("folder", NativeMethods.SHGFI.SmallIcon | NativeMethods.SHGFI.UseFileAttributes, true);
        }

        public Icon GetSmallIcon(string fileName)
        {
            return GetIcon(fileName, NativeMethods.SHGFI.SmallIcon);
        }

        public Icon GetSmallIconFromExtension(string extension)
        {
            return GetIcon(extension, NativeMethods.SHGFI.SmallIcon | NativeMethods.SHGFI.UseFileAttributes);
        }

        private Icon GetIcon(string fileName, NativeMethods.SHGFI flags, bool isFolder = false)
        {
            NativeMethods.SHFILEINFO shinfo = new();
            NativeMethods.SHGetFileInfo(fileName, isFolder ? NativeMethods.FILE_ATTRIBUTE_DIRECTORY : NativeMethods.FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(NativeMethods.SHGFI.Icon | flags));
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            NativeMethods.DestroyIcon(shinfo.hIcon);
            return icon;
        }


    }
                    */

        public class WindowInfo
        {
            public IntPtr Handle { get; init; }
            public int Pid { get; init; }
            public IntPtr ThreadId { get; init; }
            public IntPtr Parent { get; init; }
            public NM.WINDOWINFO NativeInfo { get; init; }
            public string Title { get; init; } = "";
            public bool IsVisible { get; set; }

            public override string ToString()
            {
                var g = $"X:{NativeInfo.rcWindow.Left} Y:{NativeInfo.rcWindow.Top} W:{NativeInfo.rcWindow.Width} H:{NativeInfo.rcWindow.Height}";
                var s = $"Title[{Title}] Geometry[{g}] IsVisible[{IsVisible}] Handle[{Handle}] Pid[{Pid}]";
                return s;
            }
        }
    }
}
