using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
  public class User : CanvasApi.JsonObjects.JsonObject
  {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
  }
}
