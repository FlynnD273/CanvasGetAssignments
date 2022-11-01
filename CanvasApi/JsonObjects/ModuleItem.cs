using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CanvasApi.JsonObjects
{
    public class ModuleItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Name { get; set; }
        [JsonPropertyName("module_id")]
        public int ModuleId { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("content_id")]
        public int ContentId { get; set; }
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        [JsonPropertyName("completion_requirement")]
        public CompletionRequirement CompletionRequirement { get; set; }

        public override string ToString() => Name;
    }
}
