using System.Text.Json.Serialization;

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

				[JsonIgnore]
				public Course Course { get; set; }

				[JsonIgnore]
				public Module Module { get; set; }

				public override string ToString() => $"{Name}{(Type == "Assignment" ? "*" : "")}";

				public override bool Equals(object? obj)
				{
						if (obj is ModuleItem other)
						{
								return this.Id == other.Id;
						}

						return false;
				}

				public override int GetHashCode() => Id.GetHashCode();
		}
}
