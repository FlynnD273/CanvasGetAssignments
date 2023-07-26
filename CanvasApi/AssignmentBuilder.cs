using CanvasApi.JsonObjects;
using System.Text.Json;

namespace CanvasApi
{
    public class AssignmentBuilder
    {
        private readonly CanvasApiCaller _caller;

        public AssignmentBuilder(CanvasApiCaller caller)
        {
            _caller = caller;
        }

        public async Task<Course[]> GetCoursesFromTerm(int term, IProgress<string>? progress)
        {
            string? coursesJson = coursesJson = await _caller.Call("courses?per_page=200");

            if (string.IsNullOrEmpty(coursesJson))
            {
                return Array.Empty<Course>();
            }

            // Only keep the courses from the latest term. 
            Course[] courses = JsonSerializer.Deserialize<Course[]>(coursesJson) ?? Array.Empty<Course>();

            courses = courses.Where(c => c.EnrollmentTermId == term).ToArray();

            // For cross-referencing the assignments with the module item completion status
            Dictionary<int, Assignment> contentIdToAssignment = new();

            for (int i = 0; i < courses.Length; i++)
            {
                Course course = courses[i];

                progress?.Report($"Course {i + 1}/{courses.Length}");
                course.Assignments = await GetAllCourseAssignments(course);

                foreach (Assignment assignment in course.Assignments)
                {
                    contentIdToAssignment.Add(assignment.Id, assignment);
                }

                course.Modules = await GetAllCourseModules(
																					contentIdToAssignment,
																					course,
																					$"Course {i + 1}/{courses.Length}",
																					progress);
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
            Course[]? currentCourses = JsonSerializer.Deserialize<Course[]>(coursesJson);

            if (currentCourses == null || currentCourses.Length == 0)
            {
                return Array.Empty<Course>();
            }

            return await GetCoursesFromTerm(currentCourses.Max(x => x.EnrollmentTermId), progress);
        }

        private async Task<Module[]> GetAllCourseModules(Dictionary<int, Assignment> contentIdToAssignment, Course course, string progressPrefix, IProgress<string>? progress)
        {
            string? modulesJson = await _caller.Call($"courses/{course.Id}/modules?per_page=200");

            Module[] modules = JsonSerializer.Deserialize<Module[]>(modulesJson) ?? Array.Empty<Module>();

            for (int i = 0; i < modules.Length; i++)
            {
                Module module = modules[i];
                module.Course = course;

                progress?.Report($"{progressPrefix} | Module {i + 1}/{modules.Length}");

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
