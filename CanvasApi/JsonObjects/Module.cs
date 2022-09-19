using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
    public class Module
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("items_url")]
        public string ItemsUrl { get; set; }
        [JsonPropertyName("items_count")]
        public int ItemsCount { get; set; }

        [JsonIgnore]
        public ModuleItem[] Items { get; set; }
    }
}
