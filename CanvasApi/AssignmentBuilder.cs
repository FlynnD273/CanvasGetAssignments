using CanvasApi.JsonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CanvasApi
{
    public class AssignmentBuilder
    {
        private readonly CanvasApiCaller _caller;

        public AssignmentBuilder(CanvasApiCaller caller)
        {
            _caller = caller;
        }

        public string CoursesToString(IEnumerable<Course> courses)
        {
            StringBuilder sb = new();

            foreach (Course course in courses)
            {
                sb.AppendLine($"== {course.Name} ==");
                sb.AppendLine();
                sb.AppendLine("| Assignments");

                // Print out assignment due dates
                foreach (Assignment assignment in course.Assignments)
                {
                    string due = "NO DUE DATE";
                    if (assignment.DueAt != null)
                    {
                        due = assignment.DueAt.Value.ToString("MM-dd ddd");
                    }

                    sb.AppendLine($"| | {assignment.Name} - Due:{due}");
                }

                sb.AppendLine();
                sb.AppendLine("| Modules");

                foreach (Module module in course.Modules)
                {
                    sb.AppendLine($"| | {module.Name}");

                    foreach (ModuleItem item in module.Items)
                    {
                        sb.AppendLine($"| | | {item.Name}{(item.Type == "Assignment" ? "*" : "")}");
                    }
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("* This is an assignment");
            return sb.ToString();
        }

        public async Task<Course[]> GetCoursesFromTerm(int term, IProgress<string> progress)
        {
            string? coursesJson = coursesJson = await _caller.Call("courses?per_page=200");

            if (string.IsNullOrEmpty(coursesJson))
            {
                return Array.Empty<Course>();
            }

            // Filter courses by term id. Only keep the courses from the latest term. 
            Course[] courses = JsonSerializer.Deserialize<Course[]>(coursesJson) ?? Array.Empty<Course>();

            courses = courses.Where(c => c.EnrollmentTermId == term).ToArray();

            // For cross-referencing the assignments with the module item completion status
            Dictionary<int, Assignment> contentIdToAssignment = new();

            for (int i = 0; i < courses.Length; i++)
            {
                Course course = courses[i];

                progress.Report($"Course {i + 1}/{courses.Length}");
                course.Assignments = await GetAllCourseAssignments(course);

                foreach (Assignment assignment in course.Assignments)
                {
                    contentIdToAssignment.Add(assignment.Id, assignment);
                }

                course.Modules = await GetAllCourseModules(contentIdToAssignment, course, $"Course {i+1}/{courses.Length}", progress);
            }

            return courses;
        }

        public async Task<Course[]> GetCoursesFromLatestTerm(IProgress<string> progress)
        {
            string? coursesJson = coursesJson = await _caller.Call("courses?per_page=200");

            if (string.IsNullOrEmpty(coursesJson))
            {
                return Array.Empty<Course>();
            }

            // Filter courses by term id. Only keep the courses from the latest term. 
            Course[] currentCourses = JsonSerializer.Deserialize<Course[]>(coursesJson) ?? Array.Empty<Course>();

            if (currentCourses.Length == 0)
            {
                return Array.Empty<Course>();
            }

            return await GetCoursesFromTerm(currentCourses.Max(x => x.EnrollmentTermId), progress);
        }

        private async Task<Module[]> GetAllCourseModules(Dictionary<int, Assignment> contentIdToAssignment, Course course, string progressPrefix, IProgress<string> progress)
        {
            string? modulesJson = await _caller.Call($"courses/{course.Id}/modules?per_page=200");

            Module[] modules = JsonSerializer.Deserialize<Module[]>(modulesJson) ?? Array.Empty<Module>();

            for (int i = 0; i < modules.Length; i++)
            {
                Module module = modules[i];
                module.Course = course;

                progress.Report($"{progressPrefix} | Module {i+1}/{modules.Length}");

                module.Items = await GetAllModuleItems(contentIdToAssignment, module);
            }

            return modules;
        }

        private async Task<ModuleItem[]> GetAllModuleItems(Dictionary<int, Assignment> contentIdToAssignment, Module module)
        {
            string? itemsJson = await _caller.Call($"courses/{module.Course.Id}/modules/{module.Id}/items?per_page=200");

            ModuleItem[] moduleItems = JsonSerializer.Deserialize<ModuleItem[]>(itemsJson) ?? Array.Empty<ModuleItem>();

            foreach (ModuleItem item in moduleItems.Where(x => x.Type == "Assignment"))
            {
                item.Course = module.Course;
                item.Module = module;

                if (contentIdToAssignment.TryGetValue(item.ContentId, out Assignment? assignment) && assignment != null)
                {
                    assignment.ModuleItem = item;
                }
            }

            return moduleItems;
        }

        private async Task<Assignment[]> GetAllCourseAssignments(Course course)
        {
            string? assignmentJson = await _caller.Call($"courses/{course.Id}/assignments?per_page=200");

            Assignment[] assignments = JsonSerializer.Deserialize<Assignment[]>(assignmentJson) ?? Array.Empty<Assignment>();

            foreach (Assignment a in assignments)
            {
                a.Course = course;
            }

            return assignments;
        }
    }
}
