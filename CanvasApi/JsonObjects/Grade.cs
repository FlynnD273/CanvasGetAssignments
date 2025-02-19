using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
  /**
  "grades": {
    "html_url": "https://canvas.wpi.edu/courses/41587/grades/36737",
    "current_grade": null,
    "current_score": null,
    "final_grade": null,
    "final_score": null
  },
   */
  public class Grade : CanvasApi.JsonObjects.JsonObject
  {
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }
    [JsonPropertyName("current_score")]
    public double? CurrentScore { get; set; }
    [JsonPropertyName("current_grade")]
    public string? CurrentGrade { get; set; }
  }
}
