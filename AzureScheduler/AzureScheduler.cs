using System;
using System.Threading;

namespace Scheduler
{
    public static class Program
    {
        /// <summary>
        /// Kicks off our Microsoft Solutions 
        /// Azure Storage Quartz jobs. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            try
            {
                while (true)
                {
                    var azureJobs = new Scheduler.ScheduledJob();
                    azureJobs.Run();

                    Console.WriteLine(@"{0}Check Quartz.net\Trace\application.log.txt for Job updates{0}",
                                        Environment.NewLine);

                    Console.WriteLine("{0}Press Ctrl^C to close the window. The job will continue " +
                                        "to run via Quartz.Net windows service, " +
                                        "see job activity in the Quartz.Net Trace file...{0}",
                                        Environment.NewLine);

                    Thread.Sleep(10000 * 100000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: {0}", ex.Message);
                Console.ReadKey();
            }
        }
    }
}
