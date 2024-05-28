using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using System.Threading;



namespace Splunk
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int ret = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var spl = new Splunk();
            ret = spl.Run(args, Console.Out);

            sw.Stop();

            Debug.WriteLine($"dur::::{sw.ElapsedMilliseconds}"); // 25
            Console.WriteLine($"dur::::{sw.ElapsedMilliseconds}");

            Environment.ExitCode = ret;
        }
    }
}
