
using CanvasGetAssignments;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

class Program
{
    private static readonly HttpClient client = new();
    private static string _notePath;
    private static string _header;
    private static string _canvasApiKey;

    static async Task Main(string[] args)
    {
        string[] settings = new string[] { };
        if (File.Exists("settings.txt"))
        {
            settings = File.ReadAllLines("settings.txt");
        }
        else
        {
            File.WriteAllText("settings.txt", "API Key: <Paste your Canvas API key here>\n" +
                                              "Output Path: <Path to the text file to output to>\n" +
                                              "Header: <The line of text to add the assignments afer>\n");
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
            _notePath = path;
        }
        if (settingsDict.TryGetValue("Header", out string header))
        {
            _header = header;
        }

        // Get enrolled courses
        string courseJson = await _CanvasAPICall("courses?per_page=200");

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
            string assignmentJson = await _CanvasAPICall($"courses/{course.Id}/assignments?per_page=200");
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
            string modulesJson = await _CanvasAPICall($"courses/{course.Id}/modules?per_page=200");
            course.Modules = JsonSerializer.Deserialize<Module[]>(modulesJson);

            foreach (Module module in course.Modules)
            {
                Console.WriteLine($"| | {module.Name}");

                // Get all items in the module
                string itemsJson = await _CanvasAPICall($"courses/{course.Id}/modules/{module.Id}/items?per_page=200");
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
        List<string> fileContents = File.ReadAllLines(_notePath).ToList();
        int index = fileContents.FindIndex(0, fileContents.Count, x => x == _header);

        if (index < 0) return;

        StringBuilder sb = new();

        // Only replace content after the "Assignments" header
        for (int i = 0; i <= index; i++)
        {
            sb.AppendLine(fileContents[i]);
        }
        sb.AppendLine();

        // Find all uncompleted future assignments
        IEnumerable<Assignment> assignments = currentCourses.SelectMany(x => x.Assignments).Where(x => !x.Submitted && x.DueAt > DateTime.Now && (!contentIdToModuleItem.ContainsKey(x.Id) || !(contentIdToModuleItem[x.Id]?.CompletionRequirement?.IsCompleted ?? false)));

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
                sb.AppendLine($"- [ ] [{assignment.Name}]({assignment.HtmlUrl}) [due::{assignment.DueAt.ToString("MM/dd ddd")}]  ");
            }
            Console.WriteLine();

            sb.AppendLine();
        }

        // Write back to the todo file
        File.WriteAllText(_notePath, sb.ToString());

        Quit();
    }

    private static void Quit()
    {
        Console.WriteLine("Done. Press Enter to exit...");
        Console.ReadLine();
        Environment.Exit(0);
    }

    private static async Task<string> _CanvasAPICall(string call)
    {
        HttpRequestMessage request = new()
        {
            RequestUri = new(@"https://canvas.wpi.edu/api/v1/" + call),
            Method = HttpMethod.Get,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _canvasApiKey);

        var result = await client.SendAsync(request);
        var content = await result.Content.ReadAsStringAsync();
        return content;
    }
}

