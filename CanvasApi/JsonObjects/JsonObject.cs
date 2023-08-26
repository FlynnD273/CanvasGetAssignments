using System.Text;
using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
		public class JsonObject
		{
					[JsonIgnore]
					public string JsonContent { get; set; }
		}
}

