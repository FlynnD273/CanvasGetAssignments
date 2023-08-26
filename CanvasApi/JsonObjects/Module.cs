using System.Text;
using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
    public class Module : CanvasApi.JsonObjects.JsonObject
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

				[JsonIgnore]
				public Course Course { get; set; }

				public override string ToString()
				{
					StringBuilder sb = new();
					sb.AppendLine("Module: " + Name);

					foreach (ModuleItem item in Items)
					{
							sb.AppendLine("\t" + item.ToString());
					}

					return sb.ToString();
				}

				public override bool Equals(object? obj)
				{
						if (obj is Module other)
						{
								return this.Id == other.Id;
						}

						return false;
				}

				public override int GetHashCode() => Id.GetHashCode();
		}
}
