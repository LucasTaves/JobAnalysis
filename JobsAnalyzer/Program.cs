using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.Win32.TaskScheduler;
using MoreLinq;
using MoreLinq.Extensions;
using SQLServerJobUtility;
using Task = System.Threading.Tasks.Task;

namespace JobsAnalyzer
{
    internal class Program
    {
        // full file path in 2 groups
        // ((?:[\w]\:|\\|\\\\)(?:\\[\w\-\s\d\*\%\.]+\\*)+)((?:[\w\-\s\d\*\%\.]+)+(?:\.[\w\d]+)+)
        private static readonly string separator = "-;;-";
        private static readonly Regex fileNameRegex = new Regex(
            @"((?:[\w]\:|\\|\\\\)?(?:\\[\w\-\s\d\*\%\.]+)+)\\((?:[\w\-\s\d\*\%\.]+)+(?:\.[\w\d]+)+)",
            RegexOptions.Multiline);
        private static readonly Regex pathRegex = new Regex(
            @"(([a-z]|[A-Z]):(?=\\(?![\0-\37<>:""/\\|?*])|\/(?![\0-\37<>:""/\\|?*])|$)|^\\(?=[\\\/][^\0-\37<>:""/\\|?*]+)|^(?=(\\|\/)$)|^\.(?=(\\|\/)$)|^\.\.(?=(\\|\/)$)|^(?=(\\|\/)[^\0-\37<>:""/\\|?*]+)|^\.(?=(\\|\/)[^\0-\37<>:""/\\|?*]+)|^\.\.(?=(\\|\/)[^\0-\37<>:""/\\|?*]+))((\\|\/)[^\0-\37<>:""/\\|?*]+|(\\|\/)$)*()",
            RegexOptions.Multiline);
        private static readonly Regex keyValuePairRegex = new Regex(
            @"(?<name>\S+)\s*=\s*(?<val>[^;]+?)\s*(;|$)",
            RegexOptions.Multiline);
        private static readonly Regex emailRegex = new Regex(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.Multiline);

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

            // parameters.Get("Press enter to finish...");

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
            MoreEnumerable.ForEach(
                EnvironmentDics.EnvironmentById,
                pair => Console.WriteLine($"[{pair.Key}] = {pair.Value}"));
            var env = parameters.Get(string.Empty);
            var envId = int.Parse(env);

            Console.WriteLine("Fetching all environment tasks: ");
            var environmentTasks = TaskService.Instance.AllTasks.Where(
                task => EnvironmentDics.TaskClientsByEnvironment[EnvironmentDics.EnvironmentById[envId]]
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

            ForEachExtension.ForEach(
                foundFilesByTask,
                pair =>
                {
                    var task = pair.Key;
                    var filePaths = pair.Value;

                    AnalyzeFileForTask(filePaths, stringBuilder, task);
                });

            File.WriteAllText("TasksFiles.txt", stringBuilder.ToString());
        }

        private static void AnalyzeFileForTask(
            List<string> filePaths,
            StringBuilder stringBuilder,
            Microsoft.Win32.TaskScheduler.Task task)
        {
            var localFiles = new List<string>();

            filePaths.ForEach(
                filePath =>
                {
                    var fileContent = File.ReadAllText(filePath);

                    var files = fileNameRegex.Matches(fileContent).Cast<Match>().Select(match => match.Value).ToList();
                    files.ForEach(
                        file =>
                        {
                            if (!File.Exists(file))
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}file: {file} does not exist, or no access granted");
                            }
                            else
                            {
                                localFiles.Add(file);
                            }

                            var validPath = Path.GetDirectoryName(file);
                            if (!Directory.Exists(validPath))
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}path: {validPath} does not exist, or no access granted");
                            }
                        });

                    var paths = pathRegex.Matches(fileContent).Cast<Match>().Select(match => match.Value).ToList();
                    paths.ForEach(
                        path =>
                        {
                            var validPath = Path.GetDirectoryName(path);
                            if (!Directory.Exists(validPath))
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}path: {validPath} does not exist, or no access granted");
                            }
                        });

                    var emails = emailRegex.Matches(fileContent).Cast<Match>().Select(match => match.Value).ToList();
                    emails.ForEach(
                        email =>
                        {
                            stringBuilder.AppendLine(
                                $"{task.Name}{separator}{filePath}{separator}Hard coded email: {email}");
                        });

                    var assignments = keyValuePairRegex.Matches(fileContent).Cast<Match>().ToList();
                    assignments.ForEach(
                        match =>
                        {
                            var key = match.Groups["name"].Value.Replace("\n", string.Empty);
                            var value = match.Groups["val"].Value.Replace("\n", string.Empty);

                            if (key.IndexOf("catalog", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}Possible Catalog assignment: {key} = {value}");
                            }

                            if (key.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}Possible Server assignment: {key} = {value}");
                            }

                            if (key.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}Possible Data Source assignment: {key} = {value}");
                            }

                            if (key.IndexOf("Data Base", StringComparison.OrdinalIgnoreCase) >= 0
                                || key.IndexOf("DataBase", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                stringBuilder.AppendLine(
                                    $"{task.Name}{separator}{filePath}{separator}Possible Data Base assignment: {key} = {value}");
                            }
                        });
                });

            if (localFiles.Any())
            {
                AnalyzeFileForTask(localFiles, stringBuilder, task);
            }
        }

        private static void AnalyzeDbJobs(Parameters parameters)
        {
            var foundFilesByJob = new Dictionary<Job, List<string>>();

            Console.WriteLine("Please chose your environment: ");
            MoreEnumerable.ForEach(
                EnvironmentDics.EnvironmentById,
                pair => Console.WriteLine($"[{pair.Key}] = {pair.Value}"));
            var env = parameters.Get(string.Empty);
            var envId = int.Parse(env);

            var key = EnvironmentDics.EnvironmentById[envId];
            var connectionDetails = EnvironmentDics.DbCredentialsByEnv[key];

            var address = connectionDetails[0];
            var catalog = connectionDetails[1];
            var user = connectionDetails[2];
            var password = connectionDetails[3];

            var connectionString = BuildConnectionString(address, catalog, user, password);
            var sqlJob = new JobUtility();
            sqlJob.Log += (type, log) => Console.WriteLine(log);
            sqlJob.Connect(connectionString);

            Console.WriteLine("Writing all jobs to file: ");
            var stringBuilder = new StringBuilder();

            sqlJob.GetAllJobs()
                .Cast<Job>()
                .Where(job => job.Name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList()
                .ForEach(
                    job =>
                    {
                        stringBuilder.Append($"{job.Name}{separator}{job.Description}");

                        if (job.IsEnabled)
                        {
                            stringBuilder.Append($"{separator}Job is Disabled");
                        }

                        if (job.LastRunOutcome != CompletionResult.Succeeded)
                        {
                            stringBuilder.Append($"{separator}error on last execution");
                        }

                        var steps = job.JobSteps.Cast<JobStep>().ToList();
                        steps.ForEach(
                            step =>
                            {
                                if (step.LastRunOutcome != CompletionResult.Succeeded)
                                {
                                    stringBuilder.Append($"{separator}error on last Step execution {step.Name}");
                                }

                                if (step.DatabaseName.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    stringBuilder.Append($"{separator} step {step.Name} seems to be tied to a different database: {step.DatabaseName}");
                                }

                                var files = fileNameRegex.Matches(step.Command).Cast<Match>().ToList();
                                var paths = pathRegex.Matches(step.Command).Cast<Match>().ToList();
                                var emails = emailRegex.Matches(step.Command).Cast<Match>().ToList();
                                var connectionStrings = keyValuePairRegex.Matches(step.Command).Cast<Match>().ToList();
                                files.ForEach(match => stringBuilder.Append($"{separator}{match.Value}"));
                                paths.ForEach(match => stringBuilder.Append($"{separator}{match.Value}"));
                                emails.ForEach(match => stringBuilder.Append($"{separator}{match.Value} hard coded email"));
                                connectionStrings.ForEach(
                                    match =>
                                    {
                                        var name = match.Groups["name"];
                                        var value = match.Groups["val"];
                                        if (name.Value.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0
                                            || name.Value.IndexOf("catalog", StringComparison.OrdinalIgnoreCase) >= 0
                                            || name.Value.IndexOf("data source", StringComparison.OrdinalIgnoreCase) >= 0
                                            || name.Value.IndexOf("data base", StringComparison.OrdinalIgnoreCase) >= 0
                                            || name.Value.IndexOf("database", StringComparison.OrdinalIgnoreCase) >= 0
                                            || value.Value.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0
                                            || value.Value.IndexOf("catalog", StringComparison.OrdinalIgnoreCase) >= 0
                                            || value.Value.IndexOf("database", StringComparison.OrdinalIgnoreCase) >= 0
                                            || value.Value.IndexOf("data base", StringComparison.OrdinalIgnoreCase) >= 0
                                            || value.Value.IndexOf("data source", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            stringBuilder.Append(
                                                $"{separator}{name.Value.Replace("\n", string.Empty)} = {value.Value.Replace("\n", string.Empty)} possible database assignment");
                                        }
                                    });
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
                InitialCatalog = catalog,
                UserID = userName,
                Password = userPassword
            }.ConnectionString;
        }
    }
}