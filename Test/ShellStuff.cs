using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;


/*

https://learn.microsoft.com/en-us/windows/win32/shell/launch

shell.dll contains the basics:
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
*/


/*
shlwapi.dll contains a collection of functions that provide support for various shell operations, such as 
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



namespace Splunk.Test
{
    public class ShellStuff
    {
        public string ExecInNewProcess1()
        {
            string ret = "Nada";

            // https://stackoverflow.com/questions/1469764/run-command-prompt-commands

            // One:
            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd",

                Arguments = "/c echo 123 Oscar 456 | clip",
                //Arguments = "echo 123 Oscar 456 | clip & exit",

                //Arguments = "echo >>>>>>Oscar"  //"/C copy /b Image1.jpg + Archive.rar Image2.jpg"
            };
            process.StartInfo = startInfo;
            process.Start();

             process.WaitForExit(1000);
            // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
            // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
            // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
            // like assuming that your main thread is idle when it is actually stuck in WaitForExit().

            return ret;
        }


        public string ExecInNewProcess2()
        {
            string ret = "Nada";

            Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("echo >>>>>>Oscar");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit(); // wait for the process to complete before continuing and process.ExitCode
            ret = cmd.StandardOutput.ReadToEnd();

            return ret;
        }


        public List<string> DoFileAssociation()
        {
            List<string> vals = [];

            foreach (var ext in new[] { ".doc", ".txt" })
            {
                //vals.AddRange(GetFileAssociationInfo(ext));

                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.Command, ext)); // this one
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.Executable, ext));
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.FriendlyAppName, ext));
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.FriendlyDocName, ext));
            }

            return vals;
        }

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
}
