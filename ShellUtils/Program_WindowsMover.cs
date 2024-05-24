using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellUtils
{
    // Windows Mover
    // Rearrange explorer (or whatever program is defined in config) windows upon opening.
    // Windows are resized and moved so that they do not overlap.
    // Upon program start, all mentioned windows are resized and moved: this applies to non-minimized windows; other program's window may hide the rearranged windows
    // CTRL ALT SHIFT W : rearrange Standard: exactly in the same way as during program start.
    // CTRL ALT SHIFT A : rearrange All: standard + minimized are now set to normal + all windows take focus upon rearrange (so they won't be hidden)
    // 

    // TODO: write config to users's folder ? (so every user has it's own config)
    // TODO: custom key combination in config 
    // TODO: rearrange in two screens if available

    public static class Program_not
    {
        static Config conf;


        // // Ensure only one playing at a time.
        // var fn = args[0];
        // var proc = Process.GetCurrentProcess();
        // var pname = proc.ProcessName;
        // var procs = Process.GetProcessesByName(pname);

        // if (procs.Length == 1)
        // {
        //     // I'm the first, start normally by passing the file name.
        //     Application.EnableVisualStyles();
        //     Application.SetCompatibleTextRenderingDefault(false);
        //     Application.Run(new Transport(fn));
        //     _log.Write($"main thread exit");
        // }
        // else
        // {
        //     // The second instance.
        // }



        //[STAThread]
        static void Main_not(string[] args)
        {
            using (Mutex mutex = new Mutex(false, "WindowsMover one instance only"))
            {
                if (mutex.WaitOne(300))
                {
                    conf = Config.Initialize();
                    conf.Log("WindowsMover starting");
                    InfoLog();
                    ArrangeSelectedWindows();
                    ArrangeOnOpen();
                }
                else
                {
                    if (MessageBox.Show("WindowsMover is already running\nDo you wish to terminate it?", "WindowsMover", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        RemoveAllInstancesFromMemory();
                    }
                }
            }
        }

        public class windowInfo
        {
            public IntPtr handle;
            public IntPtr parent;
            public WindowsApi.RECT rect;
            public WindowsApi.WINDOWINFO wininfo;
            public int processPid;
            public string title;

            public static List<windowInfo> GetActiveFloatingExplorerWindows(List<windowInfo> windowsList)
            {
                List<IntPtr> parents = (from windowInfo wi in windowsList
                                        where wi.parent != IntPtr.Zero
                                        select wi.parent).ToList();
                IntPtr shell = WindowsApi.GetShellWindow();
                List<windowInfo> result = (from windowInfo wi in windowsList
                                           where wi.parent == IntPtr.Zero        // must have no parent (--> remove start menu)
                                             && wi.handle != shell               // must be no shell (windows desktop)
                                             && !parents.Contains(wi.handle)     // must not be a parent (--> remove menu bar)
                                           select wi).ToList();
                return result;
            }
        }


        /// <summary>
        /// Removes from memory all instances of WindowsMovers
        /// </summary>
        public static void RemoveAllInstancesFromMemory()
        {
            Process[] processes = Process.GetProcessesByName("WindowsMover");
            int currentPID = Process.GetCurrentProcess().Id;
            foreach (Process p in processes)
            {
                try
                {
                    if (p.Id != currentPID)
                        p.Kill();
                }
                catch
                {
                    MessageBox.Show("Unable to stop WindowsMover process", "Error");
                }
            }
        }

        /// <summary>
        /// Installs a hook to intercept the creation of all windows
        /// </summary>
        public static void ArrangeOnOpen()
        {
            conf.Log("WindowsMover: installing Windows hook");
            WindowsHookForm whf = new WindowsHookForm();
            whf.WindowCreatedEvent += (data) => { ArrangeOneWindow(data); };

            if (conf.enableKeyboardShortcutsToArrangeWindows)
            {
                whf.KeypressArrangeVisibleEvent += Whf_KeypressArrangeVisibleEvent;
                whf.KeypressArrangeAllEvent += Whf_KeypressArrangeAllEvent;
            }
            
            while (true)
            {
                Application.DoEvents();
                Thread.Sleep(200);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private static void Whf_KeypressArrangeAllEvent()
        {
            ArrangeSelectedWindows(true);
        }

        private static void Whf_KeypressArrangeVisibleEvent()
        {
            ArrangeSelectedWindows(false);
        }

        /// <summary>
        /// Get a list of IntPtr handlers to all the visible windows
        /// </summary>
        /// <returns></returns>
        private static List<IntPtr> GetAllWindowsHandlers()
        {
            List<IntPtr> result = new List<IntPtr>();
            WindowsApi.EnumThreadWindowsCallback addWindowHandle = (IntPtr hWnd, IntPtr param) =>
            {
                if (WindowsApi.IsWindowVisible(hWnd))
                {
                    result.Add(hWnd);
                }
                return true;
            };

            WindowsApi.EnumWindows(addWindowHandle, IntPtr.Zero);
            return result;
        }

        /// <summary>
        /// Get informations on all Exporer opened windows 
        /// </summary>
        /// <returns>A list of informations</returns>
        private static List<windowInfo> GetActiveSelectedWindowsInfo(bool onlyTitledWindows = true)
        {
            List<windowInfo> result = new List<windowInfo>();
            List<IntPtr> visibleWindowHandles = GetAllWindowsHandlers();

            // get all running processes for the process names list in the config file
            List<Process> processesList = new List<Process>();
            foreach (string procName in conf.ProcessesToWatch)
            {
                Process[] pa = Process.GetProcessesByName(procName);
                processesList.AddRange(pa);
            }

            // Get all selected processes instances as a list of PIDs
            List<int> processesPids = (from Process exp in processesList select exp.Id).ToList();

            foreach (IntPtr handle in visibleWindowHandles)
            {
                IntPtr pid = IntPtr.Zero;
                IntPtr threadId = WindowsApi.GetWindowThreadProcessId(handle, out pid);

                if (processesPids.Contains(pid.ToInt32()))
                {
                    windowInfo wi = new windowInfo();
                    wi.handle = handle;
                    WindowsApi.GetWindowRect(handle, out wi.rect);
                    wi.parent = WindowsApi.GetParent(handle);
                    wi.wininfo = WindowsApi.WINDOWINFO.GetNewWindoInfo();

                    StringBuilder sb = new StringBuilder(1024);
                    WindowsApi.GetWindowText(wi.handle, sb, sb.Capacity);
                    wi.title = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(wi.title) || !onlyTitledWindows)
                    {
                        WindowsApi.GetWindowInfo(handle, ref wi.wininfo);
                        wi.processPid = pid.ToInt32();
                        result.Add(wi);
                    }
                }
            }
            result = windowInfo.GetActiveFloatingExplorerWindows(result);

            return result;
        }

        /// <summary>
        /// Checks if a point on the screen is inside a rectangle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="rect"></param>
        /// <param name="includeBorder"></param>
        /// <returns></returns>
        private static bool IsInRect(int x, int y, WindowsApi.RECT rect, bool includeBorder = false)
        {
            bool result = false;
            if (includeBorder)
            {
                result = (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom);
            }
            else
            {
                result = (x > rect.Left && x < rect.Right && y > rect.Top && y < rect.Bottom);
            }
            return result;
        }

        /// <summary>
        /// Checks if two rectangles have a common area
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <param name="includeBorder"></param>
        /// <returns></returns>
        private static bool IsRectOverlapping(WindowsApi.RECT r1, WindowsApi.RECT r2, bool includeBorder = false)
        {
            bool isOutside;
            if (includeBorder)
            {
                isOutside = r1.Right < r2.Left || r1.Left > r2.Right || r1.Top > r2.Bottom || r1.Bottom < r2.Top;
            }
            else
            {
                isOutside = r1.Right <= r2.Left || r1.Left >= r2.Right || r1.Top >= r2.Bottom || r1.Bottom <= r2.Top;
            }
            return !isOutside;
        }

        private static bool ContainsWithWildcards(List<string> list, string key)
        {
            bool result = false;
            result = (from string i in list
                      where (i.EndsWith("*") ? string.Compare(i, 0, key, 0, i.Length - 1) : string.Compare(i, key)) == 0
                      select 1).Any();
            return result;
        }

        /// <summary>
        /// Moves/resizes the window identified by 'newhandle'
        /// </summary>
        /// <param name="newhandle"></param>
        private static void ArrangeOneWindow(IntPtr newhandle)
        {
            List<windowInfo> wlist = GetActiveSelectedWindowsInfo(conf.onlyTitledWindows);
            windowInfo newWindow = (from windowInfo wi in wlist
                                    where wi.handle == newhandle
                                    select wi).FirstOrDefault();
            if (newWindow != null && !ContainsWithWildcards(conf.ExcludedWindowsTitles, newWindow.title))
            {
                conf.Log(string.Format("Processing window: {0}", newWindow.title));

                if (conf.doResizeOnly)
                {
                    WindowsApi.MoveWindow(newWindow.handle, newWindow.rect.Left, newWindow.rect.Top, conf.width, conf.height, true);
                }
                else // resize AND move
                {
                    System.Drawing.Rectangle screen = Config.ScreenBounds();
                    wlist.Remove(newWindow);
                    bool isAdjacentFree = false;

                    if (conf.canAppendToNext)
                    {
                        foreach (windowInfo wi in wlist)
                        {
                            WindowsApi.RECT adjacent = new WindowsApi.RECT() { Top = wi.rect.Top, Left = wi.rect.Right + conf.horizontalGap, Bottom = wi.rect.Top + conf.height, Right = wi.rect.Right + 1 + conf.width };

                            if (adjacent.Right <= screen.Right && adjacent.Bottom <= screen.Bottom)
                            {
                                isAdjacentFree = true;
                                foreach (windowInfo wi2 in wlist)
                                {
                                    if (wi2.handle != wi.handle)
                                    {
                                        if (IsRectOverlapping(wi2.rect, adjacent))
                                        {
                                            isAdjacentFree = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (isAdjacentFree)
                            {
                                WindowsApi.MoveWindow(newWindow.handle, adjacent.Left, adjacent.Top, conf.width, conf.height, true);
                                break;
                            }
                        }
                    }

                    if (!isAdjacentFree) // if no window with adjacent space
                    {
                        bool foundSuitablePosition = false;

                        if (conf.canArrangeFixedPositions)
                        {
                            WindowsApi.RECT newpos = new WindowsApi.RECT() { Left = conf.startLeft, Top = conf.startTop, Right = conf.width, Bottom = conf.height };
                            while (newpos.Bottom < screen.Bottom)
                            {
                                foundSuitablePosition = false;
                                newpos.Left = conf.startLeft;
                                newpos.Right = newpos.Left + conf.width;
                                while (newpos.Right < screen.Right)
                                {
                                    bool isFreeSpace = true;
                                    foreach (windowInfo wi in wlist)
                                    {
                                        if (IsRectOverlapping(newpos, wi.rect))
                                        {
                                            isFreeSpace = false;
                                        }
                                    }
                                    if (isFreeSpace)
                                    {
                                        foundSuitablePosition = true;
                                        WindowsApi.MoveWindow(newWindow.handle, newpos.Left, newpos.Top, newpos.Width, newpos.Height, true);
                                        break;
                                    }
                                    newpos.Left += conf.width + conf.horizontalGap;
                                    newpos.Right = newpos.Left + conf.width;
                                }
                                if (foundSuitablePosition)
                                    break;
                                newpos.Top += conf.height + conf.verticalGap;
                                newpos.Bottom = newpos.Top + conf.height;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resize and moves all selected windows already opened
        /// </summary>
        public static void ArrangeSelectedWindows(bool showMinimizedWindows = false)
        {
            conf.Log("WindowsMover: processing existing windows");

            conf = Config.Initialize();

            List<windowInfo> explorerWindows = GetActiveSelectedWindowsInfo(conf.onlyTitledWindows);

            #region Resize and move explorer windows
            if (showMinimizedWindows)
            {
                foreach (windowInfo wi in explorerWindows)
                {
                    // WindowsApi.ShowWindowAsync(wi.handle, (int)WindowsApi.WindowShowState.SW_SHOWNORMAL);
                    WindowsApi.ShowWindow(wi.handle, (int)WindowsApi.WindowShowState.SW_SHOWNORMAL);
                    // WindowsApi.SetForegroundWindow(wi.handle);
                }
            }

            explorerWindows.Reverse();
            foreach (windowInfo wi in explorerWindows)
            {
                if (conf.CanMove)
                {
                    WindowsApi.MoveWindow(wi.handle, conf.left, conf.top, conf.width, conf.height, true);
                    WindowsApi.SetForegroundWindow(wi.handle);
                }
                conf.CalculateNextPoint();
            }

            #endregion

        }

        public static void InfoLog()
        {
            Process[] allProcesses = Process.GetProcesses();
            IEnumerable<string> procNames = (from Process p in Process.GetProcesses()
                                             orderby p.ProcessName
                                             select p.ProcessName).Distinct();

            conf.Log("Currently active processes:");
            foreach (string sp in procNames)
            {
                conf.Log(sp);
            }
            conf.Log("---------------------------");
        }
    }



    public class Config
    {
        public const string configFile = "WindowsMover.config";


        public Config()
        {
            width = 346;
            height = 460;
            startLeft = 0;
            startTop = 0;
            left = startLeft;
            top = startTop;
            doResizeOnly = false;
            horizontalGap = 0;
            verticalGap = 0;
            canAppendToNext = false;
            canArrangeFixedPositions = true;
            ExcludedWindowsTitles = new List<string>();
    //     <string>Control Panel\All Control Panel Items\Personalization</string>
    //     <string>Control Panel\All Control Panel Items\System</string>
    //     <string>Control Panel\All Control Panel Items</string>
    //     <string>Shut Down Windows</string>
            ProcessesToWatch = new List<string>();
    //     <string>explorer</string>
            logFileName = "WindowsMover.log";
            doLogFile = false;
            onlyTitledWindows = true;
            enableKeyboardShortcutsToArrangeWindows = true;
        }

        public int width;
        public int height;
        public int startLeft;
        public int startTop;
        public int left;
        public int top;
        public int horizontalGap;
        public int verticalGap;

        public bool doResizeOnly;
        public bool canArrangeFixedPositions;
        public bool canAppendToNext;

        public List<string> ExcludedWindowsTitles;
        public List<string> ProcessesToWatch;

        public string logFileName;
        public bool doLogFile;
        public bool onlyTitledWindows;

        public bool enableKeyboardShortcutsToArrangeWindows;

        public bool CanMove
        {
            get
            {
                Form f = new Form();
                System.Drawing.Rectangle screen = Screen.FromControl(f).Bounds;
                f.Dispose();
                return (left + width < screen.Width) && (top + height < screen.Height);
            }
        }

        public static System.Drawing.Rectangle ScreenBounds()
        {
            Form f = new Form();
            System.Drawing.Rectangle screen = Screen.FromControl(f).Bounds;
            f.Dispose();
            return screen;
        }

        public void CalculateNextPoint()
        {
            System.Drawing.Rectangle screen = ScreenBounds();

            if ((left + 2 * width) < screen.Width)
            {
                left += (width + horizontalGap);
            }
            else
            {
                left = startLeft;
                top += (height + verticalGap);
            }
        }

        public static Config Initialize()
        {
            Config conf = LoadFromXMLFile<Config>(configFile);
            try
            { conf.SaveToFileXml(configFile); }
            catch { }
            return conf;
        }

        public void Log(string s)
        {
            if (doLogFile) // Log windows title
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0:yyyy.MM.dd HH:mm:ss} - ", DateTime.Now);
                    sb.AppendLine(s);
                    System.IO.File.AppendAllText(logFileName, sb.ToString());
                }
                catch { }
            }
        }
    }
}

