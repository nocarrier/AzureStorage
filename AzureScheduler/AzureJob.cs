using System;
using AzureManager;
using log4net;
using Quartz;

namespace Scheduler
{
    /// <summary>
    /// This class contains the actual concrete logic to call
    /// the azure storage api clean up and sync blob storage
    /// withing the execute command below. 
    /// </summary>
    public class AzureJob : IJob
    {

        private static readonly ILog Log = LogManager.GetLogger(typeof(AzureJob));

        /// <summary> 
        /// Empty constructor for job initilization
        /// <para>
        /// Quartz requires a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public AzureJob() {}

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //
                // Call our Blob Manager and 
                // copy from Nodes to Hub:

                BlobManager.CopyBlobData();

                //
                // Job Logging:

                Log.DebugFormat("{0}****{0}Azure Job {1} fired @ {2} next scheduled for {3}{0}***{0}",
                    Environment.NewLine,
                    context.JobDetail.Key,
                    context.FireTimeUtc.Value.ToString("r"),
                    context.NextFireTimeUtc.Value.ToString("r"));


                Log.DebugFormat("{0}***{0}Blob-Job has completed successfully.{0}***{0}", Environment.NewLine);
            }
            catch (Exception ex)
            {
                Log.DebugFormat(
                    "{0}***{0}Blob-Job has Failed miserably due to the following problem: {1}{0}***{0}"
                    , Environment.NewLine
                    , ex.Message);
            }
        }
    }
}