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
            _ = AssocQueryString(AssocF.Verify, assocStr, ext, verb, null, ref pcchOut);
            StringBuilder pszOut = new((int)pcchOut);
            _ = AssocQueryString(AssocF.Verify, assocStr, ext, verb, pszOut, ref pcchOut);
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
    }
}