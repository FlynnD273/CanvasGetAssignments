﻿using CanvasGetAssignments.JsonObjects;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    private static readonly HttpClient client = new();
    private static string _outputPath;
    private static string _header;
    private static string _canvasApiKey;
    private static TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private static bool _isSilent = false;
    private static bool _isSilentOnException = false;

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
    }

    static async Task Main(string[] args)
    {
        _LoadSettings(args);

        // Check for invalid output path
        if (Regex.IsMatch(_outputPath, $"[{new string(Path.GetInvalidPathChars())}]\"[{new string(Path.GetInvalidFileNameChars())}]"))
        {
            Console.WriteLine("Invalid output path name.");
            _Quit(ExitState.InvalidPathException);
        }

        // Get enrolled courses
        string courseJson = null;
        try
        {
            courseJson = await _CanvasAPICall("courses?per_page=200");
        }
        catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
        {
            _HandleCanvasApiException(e);
        }

        // Filter courses by term id. A22 term is 186
        Course[] currentCourses = JsonSerializer.Deserialize<Course[]>(courseJson);
        int currentTerm = currentCourses.Max(x => x.EnrollmentTermId);
        currentCourses = currentCourses.Where(c => c.EnrollmentTermId == currentTerm).ToArray();

        // For cross-referencing the assignments with the module item completion status
        Dictionary<int, ModuleItem> contentIdToModuleItem = new();

        foreach (Course course in currentCourses)
        {
            Console.WriteLine($"== {course.Name} ==");
            Console.WriteLine();
            Console.WriteLine("| Assignments");

            // Get all assignments for the course
            string assignmentJson = null;
            try
            {
                assignmentJson = await _CanvasAPICall($"courses/{course.Id}/assignments?per_page=200");
            }
            catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
            {
                _HandleCanvasApiException(e);
            }
            course.Assignments = JsonSerializer.Deserialize<Assignment[]>(assignmentJson);


            foreach (Assignment assignment in course.Assignments)
            {
                Console.WriteLine($"| | {assignment.Name} - Due:{assignment.DueAt.ToString("MM-dd ddd")}");

                // Set the parent course property in each assignment
                assignment.Course = course;
            }

            Console.WriteLine();
            Console.WriteLine("| Modules");

            // Get all modules in the course
            string modulesJson = null;
            try
            {
                modulesJson = await _CanvasAPICall($"courses/{course.Id}/modules?per_page=200");
            }
            catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
            {
                _HandleCanvasApiException(e);
            }

            course.Modules = JsonSerializer.Deserialize<Module[]>(modulesJson);

            foreach (Module module in course.Modules)
            {
                Console.WriteLine($"| | {module.Name}");

                // Get all items in the module
                string itemsJson = null;
                try
                {
                    itemsJson = await _CanvasAPICall($"courses/{course.Id}/modules/{module.Id}/items?per_page=200");
                }
                catch (Exception e) when (e is CanvasApiException || e is HttpRequestException)
                {
                    _HandleCanvasApiException(e);
                }

                module.Items = JsonSerializer.Deserialize<ModuleItem[]>(itemsJson);

                foreach (ModuleItem item in module.Items.Where(x => x.Type == "Assignment"))
                {
                    Console.WriteLine($"| | | {item.Title}");

                    // Add the assignment module items to the dictionary for cross-referencing
                    contentIdToModuleItem.Add(item.ContentId, item);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        // Get the contents of my todo list file
        List<string> fileContents = new();

        if (File.Exists(_outputPath))
        {
            fileContents = File.ReadAllLines(_outputPath).ToList();
        }
        else if (!string.IsNullOrEmpty(_header))
        {
            fileContents.Add(_header);
        }

        int index = fileContents.FindIndex(0, fileContents.Count, x => x == _header);
        if (string.IsNullOrEmpty(_header))
        {
            index = 0;
        }

        if (index < 0)
        {
            Console.WriteLine($"Could not find line \"{_header}\"");
            _Quit(ExitState.NoHeaderException);
        }

        StringBuilder sb = new();

        // Only replace content after the "Assignments" header
        for (int i = 0; i <= index; i++)
        {
            sb.AppendLine(fileContents[i]);
        }
        sb.AppendLine();

        // Find all uncompleted future assignments
        IEnumerable<Assignment> assignments = from course in currentCourses
                                              from assignment in course.Assignments
                                              where !assignment.Submitted &&
                                              assignment.DueAt > DateTime.Now &&
                                              (!contentIdToModuleItem.ContainsKey(assignment.Id) ||
                                                !(contentIdToModuleItem[assignment.Id]?.CompletionRequirement?.IsCompleted ?? false))
                                              orderby assignment.Course.Name, assignment.DueAt
                                              select assignment;

        Console.WriteLine("***** Future Assignments *****");
        Console.WriteLine();

        foreach (var courseGroup in assignments.GroupBy<Assignment, Course>(x => x.Course))
        {
            Console.WriteLine($"== {courseGroup.Key.Name} ==");

            // Add a header for each course
            sb.AppendLine($"#### {courseGroup.Key.Name}");
            sb.AppendLine();

            foreach (var assignment in courseGroup)
            {
                Console.WriteLine($"| {assignment.Name}");

                // Add the assignment as a checkbox so I can check off items temporarily
                sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::{TimeZoneInfo.ConvertTimeFromUtc(assignment.DueAt, _timeZone).ToString("MM/dd ddd hh:mm tt")}]  ");
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
            File.WriteAllText("settings.txt", "API Key: <Paste your Canvas API key here>\n" +
                                              "Output Path: <Path to the text file to output to>\n" +
                                              "Header: <The line of text to add the assignments after>\n");
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

        if (settingsDict.TryGetValue("API Key", out string apiKey))
        {
            _canvasApiKey = apiKey;
        }
        if (settingsDict.TryGetValue("Output Path", out string path))
        {
            _outputPath = path;
        }
        if (settingsDict.TryGetValue("Header", out string header))
        {
            _header = header;
        }
        if (settingsDict.TryGetValue("Time Zone", out string tz) && string.IsNullOrEmpty(tz))
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

        if (string.IsNullOrEmpty(_canvasApiKey))
        {
            Console.WriteLine("No API key has been set.");
            _Quit(ExitState.ApiKeyMissingException);
        }
        if (string.IsNullOrEmpty(_outputPath))
        {
            Console.WriteLine("No output path has been set.");
            _Quit(ExitState.OutputPathMissingException);
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

    private static async Task<string> _CanvasAPICall(string call)
    {
        HttpRequestMessage request = new()
        {
            RequestUri = new(@"https://canvas.wpi.edu/api/v1/" + call),
            Method = HttpMethod.Get,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _canvasApiKey);

        HttpResponseMessage result;
        try
        {
            result = await client.SendAsync(request);
        }
        catch (HttpRequestException e)
        {
            throw;
        }

        var content = await result.Content.ReadAsStringAsync();
        if (content.StartsWith("{\"errors\":"))
        {
            throw CanvasApiException.FromJson(content);
        }
        return content;
    }
}

