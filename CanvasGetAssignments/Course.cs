using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasGetAssignments
{
    internal class Course
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }
        [JsonPropertyName("enrollment_term_id")]
        public int EnrollmentTermId { get; set; }
        [JsonPropertyName("course_code")]
        public string CourseCode { get; set; }
        [JsonPropertyName("enrollments")]
        public Enrollment[] Enrollments { get; set; }

        [JsonIgnore]
        public Assignment[] Assignments { get; set; }

        [JsonIgnore]
        public Module[] Modules { get; set; }
    }
}
