using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;
using NM = Splunk.Common.NativeMethods;


namespace Splunk.Common
{
    /// <summary>Useful info about a window.</summary>
    public class WindowInfo
    {
        /// <summary>Native window handle.</summary>
        public IntPtr Handle { get; init; }

        /// <summary>Owner process.</summary>
        public int Pid { get; init; }

        /// <summary>Running on this thread.</summary>
        public IntPtr ThreadId { get; init; }

        /// <summary>Who's your daddy?</summary>
        public IntPtr Parent { get; init; }

        /// <summary>The coordinates of the window.</summary>
        public Rectangle DisplayRectangle { get { return Convert(NativeInfo.rcWindow); } }

        /// <summary>The coordinates of the client area.</summary>
        public Rectangle ClientRectangle { get { return Convert(NativeInfo.rcClient); } }

        /// <summary>Window Text.</summary>
        public string Title { get; init; } = "";

        /// <summary>This is not trustworthy as it is true for some unseen windows.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Internals if needed.</summary>
        public NM.WINDOWINFO NativeInfo { get; init; }

        /// <summary>For humans.</summary>
        public override string ToString()
        {
            var g = $"X:{DisplayRectangle.Left} Y:{DisplayRectangle.Top} W:{DisplayRectangle.Width} H:{DisplayRectangle.Height}";
            var s = $"Title[{Title}] Geometry[{g}] IsVisible[{IsVisible}] Handle[{Handle}] Pid[{Pid}]";
            return s;
        }

        Rectangle Convert(NM.RECT rect)
        {
            return new()
            {
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top
            };
        }
    }

    public class ShellUtils
    {
        /// <summary>
        /// Get all pertinent/visible windows for the application. Ignores non-visible or non-titled (internal).
        /// Note that new explorers may be in the same process or separate ones. Depends on explorer user options.
        /// </summary>
        /// <param name="appName">Application name.</param>
        /// <param name="includeAnonymous">Include those without titles or base "Program Manager".</param>
        /// <returns>List of window infos.</returns>
        public static List<WindowInfo> GetAppWindows(string appName, bool includeAnonymous = false)
        {
            List<WindowInfo> winfos = [];
            List<IntPtr> procids = [];

            // Get all processes.
            Process[] procs = Process.GetProcessesByName(appName);
            procs.ForEach(p => procids.Add(p.Id));

            // Enumerate all windows. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows
            List<IntPtr> visHandles = [];
            bool addWindowHandle(IntPtr hWnd, IntPtr param) // callback
            {
                if (NM.IsWindowVisible(hWnd))
                {
                    visHandles.Add(hWnd);
                }
                return true;
            }
            IntPtr param = IntPtr.Zero;
            NM.EnumWindows(addWindowHandle, param);

            foreach (var vh in visHandles)
            {
                var wi = GetWindowInfo(vh);

                var realWin = wi.Title != "" && wi.Title != "Program Manager";
                if (procids.Contains(wi.Pid) && (includeAnonymous || realWin))
                {
                    winfos.Add(wi);
                }
            }

            return winfos;
        }

        /// <summary>
        /// Get main window(s) for the application. Could be multiple if more than one process.
        /// </summary>
        /// <param name="appName">The app name</param>
        /// <returns>List of window handles.</returns>
        public static List<IntPtr> GetAppMainWindows(string appName)
        {
            List<IntPtr> handles = [];

            // Get all processes. There is one entry per separate process.
            // XPL: Title[] Geometry[X:0 Y: 1020 W: 1920 H: 60] IsVisible[True] Handle[131326] Pid[5748]
            Process[] procs = Process.GetProcessesByName(appName);
            // Get each main window.
            procs.ForEach(p => handles.Add(p.MainWindowHandle));
            return handles;
        }

        /// <summary>
        /// Get everything you need to know about a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>The info object.</returns>
        public static WindowInfo GetWindowInfo(IntPtr handle)
        {
            IntPtr threadId = NM.GetWindowThreadProcessId(handle, out IntPtr pid);
            NM.GetWindowRect(handle, out NM.RECT rect);

            StringBuilder sb = new(1024);
            _ = NM.GetWindowText(handle, sb, sb.Capacity);

            NM.WINDOWINFO wininfo = new();
            NM.GetWindowInfo(handle, ref wininfo);

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
