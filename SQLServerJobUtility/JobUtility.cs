using System;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace SQLServerJobUtility
{
    /// <summary>
    ///     The Job Utility Class
    /// </summary>
    public class JobUtility
    {
        /// <summary>
        ///     The server
        /// </summary>
        private Server server;

        /// <summary>
        ///     Connects to server.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>Returns weather or not a successful server connection was established</returns>
        public bool Connect(string connectionString)
        {
            Log(LogType.Info, "Attemping to connect to server.");
            try
            {
                server = new Server();
                server.ConnectionContext.ConnectionString = connectionString;
                server.ConnectionContext.Connect();
                Log(LogType.Success, "Successfully connected to serer");
                return true;
            }
            catch (SqlException ex)
            {
                Log(LogType.Failure, string.Concat("Failed to connect to serer: ", ex.Message));
                return false;
            }
        }

        /// <summary>
        ///     Gets all jobs.
        /// </summary>
        /// <returns></returns>
        public JobCollection GetAllJobs()
        {
            Log(LogType.Info, "Getting all jobs");
            return server.JobServer.Jobs;
        }

        /// <summary>
        ///     Finds the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns>Find a job by name</returns>
        public Job FindJob(string jobName)
        {
            Log(LogType.Info, string.Concat("Attempting to find job '", jobName, "'"));
            var jobCollection = GetAllJobs();

            foreach (Job job in jobCollection)
            {
                if (job.Name == jobName)
                {
                    Log(LogType.Failure, "Job Found");
                    return job;
                }
            }

            Log(LogType.Failure, "Unablet o find job");
            return null;
        }

        /// <summary>
        ///     Gets the state of the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns>
        ///     Determines a jobs current state
        /// </returns>
        public string GetJobState(string jobName)
        {
            var job = FindJob(jobName);

            if (job == null)
            {
                Log(LogType.Failure, string.Concat("Unable to find job: " + jobName));
                return "Not Found";
            }

            var jobState = job.CurrentRunStatus.ToString();
            
            Log(LogType.Success, string.Concat("Job State: " + jobState));

            return jobState;
        }

        /// <summary>
        ///     Starts the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns>Returns weather or not the job was started</returns>
        public bool StartJob(string jobName)
        {
            var job = FindJob(jobName);

            if (job == null)
            {
                Log(LogType.Failure, string.Concat("Unable to find job: " + jobName));
                return false;
            }

            if (!job.IsEnabled)
            {
                job.IsEnabled = true;
                Log(LogType.Info, string.Concat("Enabling job: " + jobName));
            }

            try
            {
                job.Start();
                Log(LogType.Success, string.Concat("Job started: " + jobName));
                return true;
            }
            catch (Exception ex)
            {
                Log(
                    LogType.Failure,
                    string.Concat("Unable to start SQL Job:", jobName, "Exception message = ", ex.Message));
                return false;
            }
        }

        /// <summary>
        ///     Stops the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns>Determines wearer or not a job was stopped</returns>
        public bool StopJob(string jobName)
        {
            var job = FindJob(jobName);

            if (job == null)
            {
                Log(LogType.Failure, string.Concat("Unable to find job: " + jobName));
                return false;
            }

            try
            {
                job.Stop();
                Log(LogType.Success, string.Concat("Job Stopped: " + jobName));
                return true;
            }
            catch (Exception ex)
            {
                Log(
                    LogType.Failure,
                    string.Concat("Unable to Stop SQL Job:", jobName, "Exception message = ", ex.Message));
                return false;
            }
        }

        /// <summary>
        ///     The log handler
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="log">The log.</param>
        public delegate void LogHandler(LogType type, string log);

        /// <summary>
        ///     Occurs when [log].
        /// </summary>
        public event LogHandler Log;
    }
}