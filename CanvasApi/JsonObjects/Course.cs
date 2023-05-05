﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
    public class Course
    {
        private int _id;
        [JsonPropertyName("id")]
        public int Id 
        { 
            get => _id; 
            set
            {
                _id = value;
                HtmlUrl = $"https://canvas.wpi.edu/courses/{_id}";
            }
        }

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
        public string HtmlUrl { get; set; }

        [JsonIgnore]
        public Assignment[] Assignments { get; set; }

        [JsonIgnore]
        public Module[] Modules { get; set; }

        public override string ToString ()
        {
            StringBuilder sb = new();

            sb.AppendLine($"== {Name} ==");
            sb.AppendLine();
            sb.AppendLine("| Assignments");

            // Print out assignment due dates
            foreach (Assignment assignment in Assignments)
            {
                sb.AppendLine($"| | {assignment}");
            }

            sb.AppendLine();
            sb.AppendLine("| Modules");

            foreach (Module module in Modules)
            {
                sb.AppendLine($"| | {module}");

                foreach (ModuleItem item in module.Items)
                {
                    sb.AppendLine($"| | | {item}");
                }
            }

            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            if (obj is Course other)
            {
                return other.Id == this.Id;
            }

            return false;
        }
    }
}
