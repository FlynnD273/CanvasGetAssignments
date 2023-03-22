using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
    public class Assignment
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("due_at")]
        public DateTime? DueAt { get; set; }
        [JsonPropertyName("allowed_attempts")]
        public int AllowedAttempts { get; set; }
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        [JsonPropertyName("has_submitted_submissions")]
        public bool Submitted { get; set; }

        [JsonIgnore]
        public Course Course { get; set; }
        [JsonIgnore]
        public ModuleItem? ModuleItem { get; set; }

        public override string ToString()
        {
            string due = "NO DUE DATE";
            if (DueAt != null)
            {
                due = DueAt.Value.ToString("MM-dd ddd");
            }

            return $"{Name} - Due:{due}";
        }

        public override int GetHashCode()
        {
            return HtmlUrl.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is Assignment other)
            {
                return this.HtmlUrl == other.HtmlUrl;
            }

            return false;
        }
    }
}
