using CanvasApi;
using CanvasApi.JsonObjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    private static string _outputPath = "";
    private static string? _header = null;
    private static string? _weeklyHeader = null;
    private static TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private static bool _isSilent = false;
    private static bool _isSilentOnException = false;
    private static AssignmentBuilder _builder;

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
        _LoadSettings(args);

        IEnumerable<Course> currentCourses = Enumerable.Empty<Course>();

        Console.WriteLine("Fetching Canvas assignments...");

        try
        {
            currentCourses = await _builder.GetCoursesFromLatestTerm(new Progress<string>(_UpdateProgress));
        }
        catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
        {
            _HandleCanvasApiException(e);
            return;
        }

        foreach (Course course in currentCourses)
        {
            Console.WriteLine(course.ToString());
            Console.WriteLine();
        }

        Console.WriteLine("* This is an assignment");
        Console.WriteLine();

        // Get the contents of my todo list file
        List<string> fileContent = new();

        if (File.Exists(_outputPath))
        {
            fileContent = File.ReadAllLines(_outputPath).ToList();
        }
        // If the file doesn't exist and there's a header defined,
        // make a new file with just the header
        else if (!string.IsNullOrEmpty(_header))
        {
            fileContent.Add(_header);
        }

        // If true, reset weekly tasks
        bool weekly = DateTime.Now.DayOfWeek == DayOfWeek.Monday;

        int headerIndex = fileContent.FindIndex(0, fileContent.Count, x => x == _header);
        int weeklyIndex = fileContent.FindIndex(0, fileContent.Count, x => x == _weeklyHeader);

        if (string.IsNullOrEmpty(_header))
        {
            headerIndex = 0;
        }

        if (headerIndex < 0)
        {
            Console.WriteLine($"Could not find line \"{_header}\"");
            _Quit(ExitState.NoHeaderException);
        }

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

        // Find all uncompleted future assignments
        IEnumerable<Assignment> assignments = from course in currentCourses
                                              from assignment in course.Assignments
                                              where !assignment.Submitted &&
                                              //(assignment.DueAt == null || assignment.DueAt > DateTime.Now) &&
                                              (assignment.ModuleItem == null ||
                                                !(assignment.ModuleItem?.CompletionRequirement?.IsCompleted ?? false))
                                              orderby assignment.Name descending
                                              orderby assignment.DueAt
                                              select assignment;

        IEnumerable<Assignment> datedAssignments = assignments.Where(x => x.DueAt != null);
        IEnumerable<Assignment> undatedAssignments = assignments.Where(x => x.DueAt == null);

        Console.WriteLine("***** Dated Assignments *****");
        Console.WriteLine();

        foreach (var courseGroup in datedAssignments.GroupBy<Assignment, Course>(x => x.Course))
        {
            Console.WriteLine($"== {courseGroup.Key.Name} ==");

            // Add a header for each course
            sb.AppendLine($"#### {courseGroup.Key.Name}");
            sb.AppendLine();

            foreach (var assignment in courseGroup)
            {
                Console.WriteLine($"| {assignment.Name}");

                string due = TimeZoneInfo.ConvertTimeFromUtc(assignment.DueAt.Value, _timeZone).ToString("ddd, MM/dd hh:mm tt");
                // Add the assignment as a checkbox so I can check off items temporarily
                sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::{due}]  ");
            }
            Console.WriteLine();

            sb.AppendLine();
        }

        Console.WriteLine("***** Undated Assignments *****");

        sb.AppendLine();
        sb.AppendLine("## Undated Assignments");
        sb.AppendLine();


        foreach (var courseGroup in undatedAssignments.GroupBy<Assignment, Course>(x => x.Course))
        {
            Console.WriteLine($"== {courseGroup.Key.Name} ==");

            // Add a header for each course
            sb.AppendLine($"#### {courseGroup.Key.Name}");
            sb.AppendLine();

            foreach (var assignment in courseGroup)
            {
                Console.WriteLine($"| {assignment.Name}");

                // Add the assignment as a checkbox so I can check off items temporarily
                sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::NO DUE DATE]  ");
            }
            Console.WriteLine();

            sb.AppendLine();
        }

        try
        {
            // Write back to the todo file
            File.WriteAllText(_outputPath, sb.ToString());
        }
        catch (IOException e)
        {
            Console.WriteLine($"Error while writing file {_outputPath}\n{e.Message}");
            _Quit(ExitState.FileWriteException);
        }

        _Quit(ExitState.Successful);
    }

    private static void _UpdateProgress(string report)
    {
        Console.Write($"\r{report}{new String(' ', 20)}");
    }

    private static void _LoadSettings(string[] args)
    {
        if (args.Contains("--silent") || args.Contains("-s"))
        {
            _isSilent = true;
        }
        if (args.Contains("--silentExceptions") || args.Contains("-se"))
        {
            _isSilent = true;
            _isSilentOnException = true;
        }

        string[] settings = new string[] { };
        if (File.Exists("settings.txt"))
        {
            settings = File.ReadAllLines("settings.txt");
        }
        else
        {
            try
            {
                File.WriteAllText("settings.txt", "API Key: <Paste your Canvas API key here>\n" +
                                              "Output Path: <Path to the text file to output to>\n" +
                                              "Header: <The line of text to add the assignments after>\n");
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error while writing file {Path.GetFullPath("settings.txt")}\n{e.Message}");
                _Quit(ExitState.FileWriteException);
            }
            
            Console.WriteLine($"Settings file not found. Refer to {Path.GetFullPath("settings.txt")} for more details");
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
            _builder = new(new(apiKey));
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
        if (settingsDict.TryGetValue("Time Zone", out string? tz) && string.IsNullOrEmpty(tz))
        {
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            }
            catch (TimeZoneNotFoundException e)
            {
                Console.WriteLine($"Time zone \"{tz}\" was not found on the local system.\n{e.Message}");
                _Quit(ExitState.TimeZoneException);
            }
        }

        if (_builder == null)
        {
            Console.WriteLine("No API key has been set.");
            _Quit(ExitState.ApiKeyMissingException);
        }

        if (string.IsNullOrEmpty(_outputPath))
        {
            Console.WriteLine("No output path has been set.");
            _Quit(ExitState.OutputPathMissingException);
        }

        if (Regex.IsMatch(_outputPath ?? "", $"[{new string(Path.GetInvalidPathChars())}]\"[{new string(Path.GetInvalidFileNameChars())}]"))
        {
            Console.WriteLine("Invalid output path name. The output path may not contain any of the following characters: " + new string(Path.GetInvalidPathChars()));
            _Quit(ExitState.InvalidPathException);
        }
    }

    private static void _HandleCanvasApiException(Exception e)
    {
        Console.WriteLine("An error has occurred while calling the Canvas API:");
        Console.WriteLine(e.Message);
        _Quit(ExitState.CanvasApiException);
    }

    private static void _Quit(ExitState exitCode)
    {
        if (!_isSilent || (exitCode != ExitState.Successful && !_isSilentOnException))
        {
            Console.WriteLine("Done. Press Enter to exit...");
            Console.ReadLine();
        }

        Environment.Exit((int)exitCode);
    }
}

