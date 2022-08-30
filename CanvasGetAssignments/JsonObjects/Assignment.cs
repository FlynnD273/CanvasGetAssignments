using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasGetAssignments.JsonObjects
{
    internal class Assignment
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("due_at")]
        public DateTime DueAt { get; set; }
        [JsonPropertyName("allowed_attempts")]
        public int AllowedAttempts { get; set; }
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        [JsonPropertyName("has_submitted_submissions")]
        public bool Submitted { get; set; }

        [JsonIgnore]
        public Course Course { get; set; }
    }
}
