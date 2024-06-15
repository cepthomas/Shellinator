using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Ephemera.NBagOfTricks;
using NM = Splunk.Common.NativeMethods;


namespace Splunk.Common
{
    /// <summary>Storage of extra shell stuff.</summary>
    public class NativeExtra
    {
        #region shlwapi.dll
        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);
        /* shlwapi.dll - a collection of functions that provide support for various shell operations, such as 
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
        #endregion

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

        public static string GetFileAssociationInfo(AssocStr assocStr, string ext, string? verb = null)
        {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, assocStr, ext, verb, null, ref pcchOut);
            StringBuilder pszOut = new((int)pcchOut);
            AssocQueryString(AssocF.Verify, assocStr, ext, verb, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        // Fancier one.
        public static string GetFileAssociationInfo2(string ext, string? verb = null)
        {
            if (ext[0] != '.')
            {
                ext = "." + ext;
            }

            string executablePath = GetFileAssociationInfo(AssocStr.Executable, ext, verb); // Will only work for 'open' verb

            if (string.IsNullOrEmpty(executablePath))
            {
                executablePath = GetFileAssociationInfo(AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

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

        //public static Icon GetSmallFolderIcon()
        //{
        //    return GetIcon("folder", SHGFI.SmallIcon | SHGFI.UseFileAttributes, true);
        //}

        //public static Icon GetSmallIcon(string fileName)
        //{
        //    return GetIcon(fileName, SHGFI.SmallIcon);
        //}

        //public static Icon GetSmallIconFromExtension(string extension)
        //{
        //    return GetIcon(extension, SHGFI.SmallIcon | SHGFI.UseFileAttributes);
        //}

        //private static Icon GetIcon(string fileName, SHGFI flags, bool isFolder = false)
        //{
        //    SHFILEINFO shinfo = new();
        //    SHGetFileInfo(fileName, isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(NativeMethods.SHGFI.Icon | flags));
        //    Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
        //    DestroyIcon(shinfo.hIcon);
        //    return icon;
        //}


        /// <summary>
        /// Make human readable.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string XlatErrorCode(int code)
        {
            // See [](C:\Program Files (x86)\Windows Kits\10\Include\10.0.22621.0\shared\winerror.h)
            switch (code)
            {
                // Basics
                case 0: return "ERROR_SUCCESS";
                case 1: return "ERROR_INVALID_FUNCTION";
                case 2: return "ERROR_FILE_NOT_FOUND";
                case 3: return "ERROR_PATH_NOT_FOUND";
                case 4: return "ERROR_TOO_MANY_OPEN_FILES";
                case 5: return "ERROR_ACCESS_DENIED";
                case 6: return "ERROR_INVALID_HANDLE";
                case 7: return "ERROR_ARENA_TRASHED";
                case 8: return "ERROR_NOT_ENOUGH_MEMORY";
                case 9: return "ERROR_INVALID_BLOCK";
                case 10: return "ERROR_BAD_ENVIRONMENT";
                case 11: return "ERROR_BAD_FORMAT";
                case 12: return "ERROR_INVALID_ACCESS";
                case 13: return "ERROR_INVALID_DATA";
                case 14: return "ERROR_OUTOFMEMORY";
                case 15: return "ERROR_INVALID_DRIVE";
                case 16: return "ERROR_CURRENT_DIRECTORY";
                case 17: return "ERROR_NOT_SAME_DEVICE";
                case 18: return "ERROR_NO_MORE_FILES";
                case 19: return "ERROR_WRITE_PROTECT";
                case 20: return "ERROR_BAD_UNIT";
                case 21: return "ERROR_NOT_READY";
                case 22: return "ERROR_BAD_COMMAND";
                case 23: return "ERROR_CRC";
                case 24: return "ERROR_BAD_LENGTH";
                case 25: return "ERROR_SEEK";
                // WinUser Error codes 1400 to 1499
                case 1400: return "ERROR_INVALID_WINDOW_HANDLE";
                case 1401: return "ERROR_INVALID_MENU_HANDLE";
                case 1402: return "ERROR_INVALID_CURSOR_HANDLE";
                case 1403: return "ERROR_INVALID_ACCEL_HANDLE";
                case 1404: return "ERROR_INVALID_HOOK_HANDLE";
                case 1405: return "ERROR_INVALID_DWP_HANDLE";
                case 1406: return "ERROR_TLW_WITH_WSCHILD";
                case 1407: return "ERROR_CANNOT_FIND_WND_CLASS";
                case 1408: return "ERROR_WINDOW_OF_OTHER_THREAD";
                case 1409: return "ERROR_HOTKEY_ALREADY_REGISTERED";
                case 1410: return "ERROR_CLASS_ALREADY_EXISTS";
                case 1411: return "ERROR_CLASS_DOES_NOT_EXIST";
                case 1412: return "ERROR_CLASS_HAS_WINDOWS";
                case 1413: return "ERROR_INVALID_INDEX";
                case 1414: return "ERROR_INVALID_ICON_HANDLE";
                case 1415: return "ERROR_PRIVATE_DIALOG_INDEX";
                case 1416: return "ERROR_LISTBOX_ID_NOT_FOUND";
                case 1417: return "ERROR_NO_WILDCARD_CHARACTERS";
                case 1418: return "ERROR_CLIPBOARD_NOT_OPEN";
                case 1419: return "ERROR_HOTKEY_NOT_REGISTERED";
                case 1420: return "ERROR_WINDOW_NOT_DIALOG";
                case 1421: return "ERROR_CONTROL_ID_NOT_FOUND";
                case 1422: return "ERROR_INVALID_COMBOBOX_MESSAGE";
                case 1423: return "ERROR_WINDOW_NOT_COMBOBOX";
                case 1424: return "ERROR_INVALID_EDIT_HEIGHT";
                case 1425: return "ERROR_DC_NOT_FOUND";
                case 1426: return "ERROR_INVALID_HOOK_FILTER";
                case 1427: return "ERROR_INVALID_FILTER_PROC";
                case 1428: return "ERROR_HOOK_NEEDS_HMOD";
                case 1429: return "ERROR_GLOBAL_ONLY_HOOK";
                case 1430: return "ERROR_JOURNAL_HOOK_SET";
                case 1431: return "ERROR_HOOK_NOT_INSTALLED";
                case 1432: return "ERROR_INVALID_LB_MESSAGE";
                case 1433: return "ERROR_SETCOUNT_ON_BAD_LB";
                case 1434: return "ERROR_LB_WITHOUT_TABSTOPS";
                case 1435: return "ERROR_DESTROY_OBJECT_OF_OTHER_THREAD";
                case 1436: return "ERROR_CHILD_WINDOW_MENU";
                case 1437: return "ERROR_NO_SYSTEM_MENU";
                case 1438: return "ERROR_INVALID_MSGBOX_STYLE";
                case 1439: return "ERROR_INVALID_SPI_VALUE";
                case 1440: return "ERROR_SCREEN_ALREADY_LOCKED";
                case 1441: return "ERROR_HWNDS_HAVE_DIFF_PARENT";
                case 1442: return "ERROR_NOT_CHILD_WINDOW";
                case 1443: return "ERROR_INVALID_GW_COMMAND";
                case 1444: return "ERROR_INVALID_THREAD_ID";
                case 1445: return "ERROR_NON_MDICHILD_WINDOW";
                case 1446: return "ERROR_POPUP_ALREADY_ACTIVE";
                case 1447: return "ERROR_NO_SCROLLBARS";
                case 1448: return "ERROR_INVALID_SCROLLBAR_RANGE";
                case 1449: return "ERROR_INVALID_SHOWWIN_COMMAND";
                case 1450: return "ERROR_NO_SYSTEM_RESOURCES";
                case 1451: return "ERROR_NONPAGED_SYSTEM_RESOURCES";
                case 1452: return "ERROR_PAGED_SYSTEM_RESOURCES";
                case 1453: return "ERROR_WORKING_SET_QUOTA";
                case 1454: return "ERROR_PAGEFILE_QUOTA";
                case 1455: return "ERROR_COMMITMENT_LIMIT";
                case 1456: return "ERROR_MENU_ITEM_NOT_FOUND";
                case 1457: return "ERROR_INVALID_KEYBOARD_HANDLE";
                case 1458: return "ERROR_HOOK_TYPE_NOT_ALLOWED";
                case 1459: return "ERROR_REQUIRES_INTERACTIVE_WINDOWSTATION";
                case 1460: return "ERROR_TIMEOUT";
                case 1461: return "ERROR_INVALID_MONITOR_HANDLE";
                case 1462: return "ERROR_INCORRECT_SIZE";
                case 1463: return "ERROR_SYMLINK_CLASS_DISABLED";
                case 1464: return "ERROR_SYMLINK_NOT_SUPPORTED";
                case 1465: return "ERROR_XML_PARSE_ERROR";
                case 1466: return "ERROR_XMLDSIG_ERROR";
                case 1467: return "ERROR_RESTART_APPLICATION";
                case 1468: return "ERROR_WRONG_COMPARTMENT";
                case 1469: return "ERROR_AUTHIP_FAILURE";
                case 1470: return "ERROR_NO_NVRAM_RESOURCES";
                case 1471: return "ERROR_NOT_GUI_PROCESS";
                default: return $"Error {code}";
            }
        }
    }
}