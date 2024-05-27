namespace Splunk.Ui
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Ensure only one playing at a time. TODO1 or don't care? See also Splunk program.cs.
            using (Mutex mutex = new(false, "Splunk_Ui_instance"))
            {
                if (mutex.WaitOne(300))
                {
                    var f = new MainForm();
                    bool hide = false;

                    if (hide)
                    {
                        // This doesn't work:
                        f.Visible = false;
                        // This fake seems to:
                        f.WindowState = FormWindowState.Minimized;
                        f.ShowIcon = false;
                        f.ShowInTaskbar = false;
                    }

                    Application.Run(f);
                }
                else
                {
                    //if (MessageBox.Show("WindowsMover is already running\nDo you wish to terminate it?", "WindowsMover", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    //{
                    //    RemoveAllInstancesFromMemory();
                    //}
                }
            }


            // /// <summary>
            // /// Installs a hook to intercept the creation of all windows
            // /// </summary>
            // // public static void ArrangeOnOpen()
            // // {
            // //conf.Log("WindowsMover: installing Windows hook");
            // WindowsHookForm whf = new WindowsHookForm();
            // whf.WindowCreatedEvent += (data) => { ArrangeOneWindow(data); };
            // // if (conf.enableKeyboardShortcutsToArrangeWindows)
            // // {
            // //     whf.KeypressArrangeVisibleEvent += Whf_KeypressArrangeVisibleEvent;
            // //     whf.KeypressArrangeAllEvent += Whf_KeypressArrangeAllEvent;
            // // }
            // while (true)
            // {
            //     Application.DoEvents();
            //     Thread.Sleep(200);
            //     GC.Collect();
            //     GC.WaitForPendingFinalizers();
            //     GC.Collect();
            // }
            // // }
        }
    }
}