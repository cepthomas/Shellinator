using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;

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

            // Ensure only one playing at a time. TODO2 or don't care? See also Splunk program.cs.
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
                    // Already running, send args to resident.
                }
            }
        }
    }
}