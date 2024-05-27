using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using System.Threading;


//TODO2 ? make into windows service like MassProcessing. Or at least run at startup.


namespace Splunk
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int ret = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //var fn = args[0];
            var proc = Process.GetCurrentProcess();
            var pname = proc.ProcessName;
            var procs = Process.GetProcessesByName(pname);

            //_log.Write($"num-procs:{procs.Length} pid:{proc.Id} arg-fn:{fn}");


            // Ensure only one playing at a time. TODO1 or don't care?
            // One way to do it:
            if (procs.Length == 1)
            {
                //_log.Write($"main thread enter");

                // I'm the first.
                var spl = new Splunk();
                ret = spl.Run(args, Console.Out);
                //Console.WriteLine("Hello, World!");

                //// I'm the first, start normally by passing the file name.
                //Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new Transport(fn));
                //_log.Write($"main thread exit");
            }
            else
            {
                //// If this is the second instance, alert the primary by connecting and sending the new file name.
                //_log.Write($"sub thread enter");

                //Client client = new(Common.PipeName, Common.LogFileName);
                //var res = client.Send(fn, 1000);

                //switch (res)
                //{
                //    case ClientStatus.Error:
                //        _log.Write($"Client error:{client.Error}", true);
                //        MessageBox.Show(client.Error, "Error!");
                //        break;

                //    case ClientStatus.Timeout:
                //        _log.Write($"Client timeout", true);
                //        MessageBox.Show("Timeout!");
                //        break;
                //}

                //_log.Write($"sub thread exit {res}");
            }

            // Another way to do it:
            using (Mutex mutex = new(false, "Splunk_instance"))
            {
                if (mutex.WaitOne(300)) // first instance
                {
                    var spl = new Splunk();
                    ret = spl.Run(args, Console.Out);
                }
                else // second instance
                {
                    //if (MessageBox.Show("WindowsMover is already running\nDo you wish to terminate it?", "WindowsMover", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    //{
                    //    RemoveAllInstancesFromMemory();
                    //}
                }
            }

            sw.Stop();

            Debug.WriteLine($"dur::::{sw.ElapsedMilliseconds}"); // 25
            Console.WriteLine($"dur::::{sw.ElapsedMilliseconds}");

            Environment.ExitCode = ret;
        }
    }
}
