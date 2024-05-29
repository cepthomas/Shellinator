using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class ShellUtils
    {
        /// <summary>
        /// Get all pertinent/visible windows for the application. Ignores non-visible or non-titled (internal).
        /// Note that new explorers may be in the same process or separate ones. Depends on explorer user options.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns>List of window infos.</returns>
        public static List<WindowInfo> GetAppWindows(string appName)
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
                if (procids.Contains(wi.Pid) && wi.Title.Length > 0)
                {
                    winfos.Add(wi);
                }
            }

            // >>> With no explorers
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
            // >>> With two explorers, 1 tab, 2 tab
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

            return winfos;
        }

        /// <summary>
        /// Get main window(s) for the application. Could be multiple if more than one process.
        /// </summary>
        /// <param name="appName"></param>
        /// <returns>List of window handles.</returns>
        public static List<IntPtr> GetAppMainWindows(string appName)
        {
            List<IntPtr> handles = [];

            // Get all processes. There is one entry per separate process (I think):
            // XPL: Title[] Geometry[X:0 Y: 1020 W: 1920 H: 60] IsVisible[True] Handle[131326] Pid[5748]
            Process[] procs = Process.GetProcessesByName(appName);
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
    }
}
