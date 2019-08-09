﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.Win32.TaskScheduler;
using SQLServerJobUtility;
using MoreEnumerable = MoreLinq.MoreEnumerable;
using Task = System.Threading.Tasks.Task;

namespace JobsAnalyzer
{
    internal class Program
    {
        private static readonly string separator = "-;;-";
        private static readonly Regex fileNameRegex = new Regex(
            @"(?:[A-Za-z]\:|\\)(\\[a-zA-Z_\-\s0-9\.]+)+\.*$",
            RegexOptions.Multiline);
        private static readonly Regex connectionStringRegex = new Regex(
            @"(?<Key>[^=;]+)=(?<Val>[^;]+)",
            RegexOptions.Multiline);

        private static readonly Dictionary<string, List<string>> taskClientsByEnvironment =
            new Dictionary<string, List<string>>
            {
                {
                    "NA6", new List<string>
                    {
                        "CAASCO"
                    }
                }
            };

        private static readonly Dictionary<int, string> environmentById = new Dictionary<int, string>
        {
            { 1, "EU1" },
            { 2, "EU2" },
            { 3, "EU3" },
            { 4, "EU5" },
            { 5, "NA3" },
            { 6, "NA6" },
            { 7, "SG1" },
            { 8, "ZAIN" }
        };

        private static async Task Main(string[] args)
        {
            var parameters = new Parameters(args);

            var action = parameters.Get("What do you want to analyze? [j]obs; [t]asks: ");

            switch (action.ToLower())
            {
                case "j":
                case "jobs":
                    AnalyzeDbJobs(parameters);
                    break;
                case "t":
                case "tasks":
                    AnalyzeTasks(parameters);
                    break;
            }

            parameters.Get("Press enter to finish...");

            // var folderName = args.Length > 0
            // ? args[0]
            // : parameters.Get("Please type folder name: ");

            // var batFiles = Directory.EnumerateFiles(folderName, "*.bat").ToList();

            // var fileNameRegex = new Regex(@"^(?:[A-Za-z]\:|\\)(\\[a-zA-Z_\-\s0-9\.]+)+\.*$", RegexOptions.Multiline);
            // var connectionStringRegex = new Regex(@"(?<Key>[^=;]+)=(?<Val>[^;]+)", RegexOptions.Multiline);

            // batFiles.ForEach(
            // fileName =>
            // {
            // var fileText = File.ReadAllText(fileName);

            // var allFiles = fileNameRegex.Matches(fileText).ToList();
            // var allConnectionStrings = connectionStringRegex.Matches(fileText).ToList();

            // using (var streamWriter = File.AppendText("E:\\OneDrive\\Desktop\\Test.txt"))
            // {
            // streamWriter.WriteLine("Files:");
            // allFiles.ForEach(match => streamWriter.WriteLine(match.Value));

            // streamWriter.WriteLine("Connection Strings:");
            // allConnectionStrings.ForEach(match => streamWriter.WriteLine(match.Value));
            // }
            // });
        }

        private static void AnalyzeTasks(Parameters parameters)
        {
            var foundFilesByTask = new Dictionary<Microsoft.Win32.TaskScheduler.Task, List<string>>();

            Console.WriteLine("Please chose your environment: ");
            MoreEnumerable.ForEach(environmentById, pair => Console.WriteLine($"[{pair.Key}] = {pair.Value}"));
            var env = parameters.Get(string.Empty);
            var envId = int.Parse(env);

            Console.WriteLine("Fetching all environment tasks: ");
            var environmentTasks = TaskService.Instance.AllTasks.Where(
                task => taskClientsByEnvironment[environmentById[envId]]
                    .Any(taskName => task.Name.IndexOf(taskName, StringComparison.OrdinalIgnoreCase) >= 0));

            Console.WriteLine("Writing all tasks to file: ");
            var stringBuilder = new StringBuilder();
            MoreEnumerable.ForEach(
                environmentTasks,
                task =>
                {
                    stringBuilder.Append($"{task.Name}{separator}{task.Definition.RegistrationInfo.Description}");
                    if (!task.Enabled)
                    {
                        stringBuilder.Append($"{separator}Task is disabled");
                    }

                    var pastMonthEvents = TaskService.Instance.GetEventLog(task.Path)
                        .Where(eventEntry => eventEntry.TimeCreated > DateTime.Now - TimeSpan.FromDays(30));

                    // if (task.LastTaskResult != 0)
                    // {
                    // stringBuilder.Append($";Last run was not successful")
                    // }
                    foreach (var ev in pastMonthEvents)
                    {
                        if (ev.StandardEventId == StandardTaskEventId.ActionFailure
                            || ev.StandardEventId == StandardTaskEventId.ScheduleServiceStartFailed
                            || ev.StandardEventId == StandardTaskEventId.JobStartFailed
                            || ev.StandardEventId == StandardTaskEventId.CompatTaskStatusUpdateFailed
                            || ev.StandardEventId == StandardTaskEventId.CompatUpgradeTaskLoadFailed
                            || ev.StandardEventId == StandardTaskEventId.JobFailure
                            || ev.StandardEventId == StandardTaskEventId.LogonFailure
                            || ev.StandardEventId == StandardTaskEventId.FailedTaskRestart
                            || ev.StandardEventId == StandardTaskEventId.MissedTaskLaunched
                            || ev.StandardEventId == StandardTaskEventId.RejectedTaskRestart
                            || ev.Level == "Warning"
                            || ev.Level == "Error")
                        {
                            stringBuilder.Append($"{separator}There was an issue running the task in the last 30 days");
                            break;
                        }
                    }

                    var actions = task.Definition.Actions;
                    MoreEnumerable.ForEach(
                        actions.OfType<ExecAction>(),
                        action =>
                        {
                            if (!File.Exists(action.Path))
                            {
                                stringBuilder.Append($"{separator}{action.Path} does not exist");
                            }
                            else
                            {
                                if (!foundFilesByTask.ContainsKey(task))
                                {
                                    foundFilesByTask[task] = new List<string>();
                                }

                                foundFilesByTask[task].Add(action.Path);
                            }
                        });
                    stringBuilder.AppendLine();
                });

            File.WriteAllText("Tasks.txt", stringBuilder.ToString());

            AnalyzeTasksFiles(foundFilesByTask);

            Console.WriteLine("Done");
        }

        // TODO: Make it recursive
        private static void AnalyzeTasksFiles(
            Dictionary<Microsoft.Win32.TaskScheduler.Task, List<string>> foundFilesByTask)
        {
            var stringBuilder = new StringBuilder();

            Console.WriteLine("Analyzing files referenced by tasks");

            MoreEnumerable.ForEach(
                foundFilesByTask,
                pair =>
                {
                    var task = pair.Key;
                    var filePaths = pair.Value;

                    filePaths.ForEach(
                        filePath =>
                        {
                            var fileContent = File.ReadAllText(filePath);

                            var files = fileNameRegex.Matches(fileContent)
                                .Cast<Match>()
                                .Select(match => match.Value)
                                .ToList();
                            files.ForEach(
                                file =>
                                {
                                    if(!File.Exists(file) && !Directory.Exists(file))
                                    {
                                        stringBuilder.AppendLine(
                                            $"{task.Name}{separator}{filePath}{separator}{file} does not exist");
                                    }
                                    
                                });
                        });
                });

            File.WriteAllText("TasksFiles.txt", stringBuilder.ToString());
        }

        private static void AnalyzeDbJobs(Parameters parameters)
        {
            var foundFilesByJob = new Dictionary<Job, List<string>>();

            var address = parameters.Get("Please provide the DB Address: ");
            var catalog = parameters.Get("Please provide the DB Catalog: ");
            var user = parameters.Get("Please provide the DB User: ");
            var password = parameters.Get("Please provide the DB Password: ");

            var connectionString = BuildConnectionString(address, catalog, user, password);
            var sqlJob = new JobUtility();
            sqlJob.Log += (type, log) => Console.WriteLine(log);
            sqlJob.Connect(connectionString);

            Console.WriteLine("Writing all jobs to file: ");
            var stringBuilder = new StringBuilder();

            sqlJob.GetAllJobs()
                .Cast<Job>()
                .ToList()
                .ForEach(
                    job =>
                    {
                        stringBuilder.Append($"{job.Name}{separator}{job.Description}");

                        if (job.IsEnabled)
                        {
                            stringBuilder.Append($"{separator}Job is Disabled");
                        }

                        var steps = job.JobSteps.Cast<JobStep>().ToList();
                        steps.ForEach(
                            step =>
                            {
                                var files = fileNameRegex.Matches(step.Command).Cast<Match>().ToList();
                                var connectionStrings =
                                    connectionStringRegex.Matches(step.Command).Cast<Match>().ToList();
                                files.ForEach(match => stringBuilder.Append($"{separator}{match.Value}"));

                                // connectionStrings.ForEach(match => stringBuilder.Append($",{match.Value}"));
                            });
                        stringBuilder.AppendLine();
                    });

            File.WriteAllText("DBJobs.txt", stringBuilder.ToString());
            Console.WriteLine("Done");
        }

        private static string BuildConnectionString(
            string dataSource,
            string catalog,
            string userName,
            string userPassword)
        {
            return new SqlConnectionStringBuilder
            {
                DataSource = dataSource,

                // InitialCatalog = catalog,
                UserID = userName,
                Password = userPassword
            }.ConnectionString;
        }
    }
}