using CanvasApi;
using CanvasApi.JsonObjects;
using CanvasGetAssignments;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    private static string _outputPath = "";
    private static string? _header = null;
    private static string? _weeklyHeader = null;
    private static TimeZoneInfo _timeZone = TimeZoneInfo.Local;
		private static int[] _termIds = Array.Empty<int>();

    private enum ExitState
    {
        Successful = 0,
        InvalidPathException,
        CanvasApiException,
        NoHeaderException,
        FileWriteException,
        TimeZoneException,
        ApiKeyMissingException,
        OutputPathMissingException,
        SettingsMissingException,
    }

    static async Task Main(string[] args)
    {
        (var builder, var settingsPath) = _LoadSettings(args);

        IEnumerable<Course> currentCourses = Enumerable.Empty<Course>();

        Console.WriteLine("Fetching Canvas assignments...");

				var progress = new Progress<string>(_UpdateProgress);
				
				var tempCourses = await builder.GetCourses(progress);
				foreach (var c in tempCourses.OrderBy(x => x.EnrollmentTermId))
				{
						Console.WriteLine($"{c.Name} | Term ID: {c.EnrollmentTermId}");
				}
        try
        {
						if (_termIds.Length == 0)
						{
								currentCourses = await builder.GetCoursesFromTerm(tempCourses.Max(x => x.EnrollmentTermId), progress);
						}
						else
						{
								currentCourses = Array.Empty<Course>();
								foreach (var termId in _termIds)
								{
										currentCourses = currentCourses.Concat(await builder.GetCoursesFromTerm(termId, progress));
								}
						}
        }
        catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
        {
            _HandleCanvasApiException(e);
            return;
        }

        Console.WriteLine();
        foreach (Course course in currentCourses)
        {
            Console.WriteLine(course.ToString());
            Console.WriteLine();
        }

        // Find all uncompleted future assignments
        IEnumerable<Assignment> assignments = from course in currentCourses
                                              from assignment in course.Assignments
																							/* where !assignment.IsLocked */
                                              where !assignment.Submitted
                                              /* where (assignment.DueAt == null || assignment.DueAt > DateTime.Now) && */
                                              where (assignment.ModuleItem == null ||
                                                    (!(assignment.ModuleItem?.CompletionRequirement?.IsCompleted ?? false)))
                                              orderby assignment.Name descending
                                              orderby assignment.DueAt
                                              select assignment;

        Dictionary<string, Assignment> linkToAssignmentDict = new();
        foreach (Assignment assignment in assignments)
        {
            linkToAssignmentDict.Add(assignment.HtmlUrl, assignment);
        }

        // Get the contents of my todo list file
        List<string> fileContent = new();

        fileContent = File.ReadAllLines(_outputPath).ToList();

        int headerIndex = 0;
        int weeklyIndex = -1;

        if (!string.IsNullOrEmpty(_weeklyHeader))
        {
            weeklyIndex = fileContent.FindIndex(0, fileContent.Count, x => x == _weeklyHeader);
        }

        // If true, reset weekly tasks
        bool weekly = DateTime.Now.DayOfWeek == DayOfWeek.Monday && weeklyIndex > 0;

        if (!string.IsNullOrEmpty(_header))
        {
            headerIndex = fileContent.FindIndex(0, fileContent.Count, x => x == _header);
        }

        if (headerIndex < 0)
        {
            Console.Error.WriteLine($"Could not find line \"{_header}\"");
            _Quit(ExitState.NoHeaderException);
        }

        HashSet<Assignment> manuallyCompletedAssignments;

				string _manuallyCompletedPath = Path.Join(settingsPath, "manuallycompleted.json");
        if (File.Exists(_manuallyCompletedPath))
        {
            using (FileStream stream = File.OpenRead(_manuallyCompletedPath))
            {
                try
                {
                    manuallyCompletedAssignments = JsonSerializer.Deserialize<HashSet<Assignment>>(stream) ?? new();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                    manuallyCompletedAssignments = new();
                }
            }

            foreach (Assignment link in manuallyCompletedAssignments)
            {
                if (!linkToAssignmentDict.ContainsKey(link.HtmlUrl))
                {
                    manuallyCompletedAssignments.Remove(link);
                }
            }
        }
        else
        {
            manuallyCompletedAssignments = new();
        }


        for (int i = headerIndex + 1; i < fileContent.Count; i++)
        {
            if (fileContent[i].StartsWith("- [x] "))
            {
                var match = Regex.Match(fileContent[i], @"(https:\/\/.*)\)");
                if (!match.Success) continue;

                foreach (Group group in match.Groups)
                {
                    if (linkToAssignmentDict.TryGetValue(group.Value, out Assignment assignment))
                    {
                        manuallyCompletedAssignments.Add(assignment);
                        break;
                    }
                }
            }
        }

				Directory.CreateDirectory(Path.GetDirectoryName(_manuallyCompletedPath));
        File.WriteAllText(_manuallyCompletedPath, JsonSerializer.Serialize(manuallyCompletedAssignments));

        StringBuilder sb = new();

        // Only replace content after the header
        int trimIndex = headerIndex;

        // If we're resetting the weekly todos, set the starting index further back
        if (weekly)
        {
            trimIndex = weeklyIndex;
        }

        for (int i = 0; i < trimIndex; i++)
        {
            sb.AppendLine(fileContent[i]);
        }

        if (weekly)
        {
            for (int i = weeklyIndex; i < headerIndex; i++)
            {
                // Clear the todo status
                sb.AppendLine(fileContent[i].Replace("[x]", "[ ]"));
            }
        }

        sb.AppendLine(_header);
        sb.AppendLine();
        sb.AppendLine($"last updated at `{DateTime.Now:ddd, MM/dd hh:mm tt}`");
        sb.AppendLine();

        IEnumerable<Assignment> datedAssignments = assignments.Where(x => x.DueAt != null && !manuallyCompletedAssignments.Contains(x));
        IEnumerable<Assignment> undatedAssignments = assignments.Where(x => x.DueAt == null && !manuallyCompletedAssignments.Contains(x));

        if (datedAssignments.Any())
        {
						IEnumerable<Assignment> duedateAssignments = datedAssignments.OrderBy(x => x.DueAt);
            sb.AppendLine();
            sb.AppendLine("## Dated Assignments By Due Date");
            sb.AppendLine();

            foreach (var assignment in duedateAssignments)
            {
                string due = TimeZoneInfo.ConvertTimeFromUtc(assignment.DueAt ?? DateTime.MinValue, _timeZone).ToString("ddd, MM/dd hh:mm tt");
                // Add the assignment as a checkbox so I can check off items temporarily
                sb.AppendLine($"- [ ] [due::{due}] [*{assignment.Course.Name}* - {assignment.Name}]({assignment.HtmlUrl})");
            }
        }

        foreach (Course course in currentCourses)
        {
            Console.WriteLine("Dated Course: " + course.Name);

            // Add a header for each course
            sb.AppendLine($"#### [{course.Name}]({Url.Combine(course.HtmlUrl, "grades")})");
            sb.AppendLine();

            foreach (var assignment in datedAssignments.Where(x => x.Course == course))
            {
                Console.WriteLine("Dated Assignment: " + assignment.Name);

                string due = TimeZoneInfo.ConvertTimeFromUtc(assignment.DueAt ?? DateTime.MinValue, _timeZone).ToString("ddd, MM/dd hh:mm tt");
                // Add the assignment as a checkbox so I can check off items temporarily
                sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::{due}]  ");
            }
            Console.WriteLine();

            sb.AppendLine();
        }

        if (undatedAssignments.FirstOrDefault() != null)
        {
            sb.AppendLine();
            sb.AppendLine("## Undated Assignments");
            sb.AppendLine();

            foreach (Course course in currentCourses)
            {
								var courseAssignments = undatedAssignments.Where(x => x.Course == course);
								if (!courseAssignments.Any()) continue;

                Console.WriteLine("Undated Course: " + course.Name);

                // Add a header for each course
                sb.AppendLine($"#### [{course.Name}]({Url.Combine(course.HtmlUrl, "grades")})");
                sb.AppendLine();

                foreach (var assignment in courseAssignments)
                {
                    Console.WriteLine($"Undated Assignment: {assignment.Name}");

                    // Add the assignment as a checkbox so I can check off items temporarily
                    sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::NO DUE DATE]  ");
                }
                Console.WriteLine();

                sb.AppendLine();
            }
        }

        try
        {
            // Write back to the todo file
            File.WriteAllText(_outputPath, sb.ToString());
        }
        catch (IOException e)
        {
            Console.Error.WriteLine($"Error while writing file {_outputPath}\n{e.Message}");
            _Quit(ExitState.FileWriteException);
        }

        _Quit(ExitState.Successful);
    }

    private static void _UpdateProgress(string report)
    {
        Console.Write($"\r{report}{new String(' ', 20)}");
    }

    private static Tuple<AssignmentBuilder, string> _LoadSettings(string[] args)
    {
        AssignmentBuilder builder = null;
        string[] settings = new string[] { };
        string settingsPath = "";

        string configPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CanvasGetAssignments");
        settingsPath = Path.Join(configPath, "settings.txt");

        if (File.Exists(settingsPath))
        {
            settings = File.ReadAllLines(settingsPath);
        }
        else
        {
            try
            {
								Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, "API Key: <Paste your Canvas API key here>\n" +
                                                                    "Output Path: <Path to the text file to output to>\n" +
                                                                    "Header: <The line of text to add the assignments after>\n" +
                                                                    "Weekly: <The line of text at the top of the weekly tasks checklist>\n" +
                                                                    "Time Zone: <Local time zone to use for due dates>\n" +
                                                                    "TermIDs: <The term IDs to include in the list. Leave bkank to use the most recent term ID>\n");
            }
            catch (IOException e)
            {
                Console.Error.WriteLine($"Error while writing file {settingsPath}");
                Console.Error.WriteLine(e.Message);
                _Quit(ExitState.FileWriteException);
            }

            Console.Error.WriteLine($"Settings not loaded. Configuration file is located at {settingsPath}");
            _Quit(ExitState.SettingsMissingException);
        }

        Dictionary<string, string> settingsDict = new();
        foreach (string line in settings)
        {
            if (line.Contains(':'))
            {
                string[] setting = line.Split(':');
                settingsDict.Add(setting[0].Trim(), string.Join(":", setting.Skip(1)).Trim());
            }
        }

        if (settingsDict.TryGetValue("API Key", out string? apiKey) && apiKey != null)
        {
            builder = new(new(apiKey));
        }
        if (settingsDict.TryGetValue("Output Path", out string? path) && path != null)
        {
            _outputPath = path;
        }
        if (settingsDict.TryGetValue("Header", out string? header) && header != null)
        {
            _header = header;
        }
        if (settingsDict.TryGetValue("Weekly", out string? weekly) && weekly != null)
        {
            _weeklyHeader = weekly;
        }
        if (settingsDict.TryGetValue("TermIDs", out string? termIds) && termIds != null)
        {
						var termIdArr = termIds.Replace(" ", "").Split(",");
            _termIds = termIdArr.Select(x => int.Parse(x)).ToArray();
        }
        if (settingsDict.TryGetValue("Time Zone", out string? tz) && string.IsNullOrEmpty(tz))
        {
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            }
            catch (TimeZoneNotFoundException e)
            {
                Console.Error.WriteLine($"Time zone \"{tz}\" was not found on the local system.");
                Console.Error.WriteLine(e.Message);
                _Quit(ExitState.TimeZoneException);
            }
        }

        if (builder == null)
        {
            Console.Error.WriteLine("No API key has been set.");
            _Quit(ExitState.ApiKeyMissingException);
        }

        if (string.IsNullOrEmpty(_outputPath))
        {
            Console.Error.WriteLine("No output path has been set.");
            _Quit(ExitState.OutputPathMissingException);
        }

        if (!File.Exists(_outputPath))
        {
            try
            {
                if (!string.IsNullOrEmpty(_header))
                {
                    File.WriteAllText(_outputPath, _header);
                }
                else
                {
                    File.Create(_outputPath);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error writing to output path");
                Console.Error.WriteLine(e.Message);
                _Quit(ExitState.InvalidPathException);
            }
        }

        string localPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CanvasGetAssignments");
        return new (builder, localPath);
    }

    private static void _HandleCanvasApiException(Exception e)
    {
        Console.Error.WriteLine("An error has occurred while calling the Canvas API:");
        Console.Error.WriteLine(e.Message);
        _Quit(ExitState.CanvasApiException);
    }

    private static void _Quit(ExitState exitCode)
    {
        Environment.Exit((int)exitCode);
    }
}

