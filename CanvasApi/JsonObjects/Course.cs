using System.Text;
using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
  public class Course : CanvasApi.JsonObjects.JsonObject
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
    public Enrollment Enrollment { get; set; } = new();

    [JsonIgnore]
    public string HtmlUrl { get; set; }

    [JsonIgnore]
    public Assignment[] Assignments { get; set; }

    [JsonIgnore]
    public Module[] Modules { get; set; }

    public override string ToString()
    {
      StringBuilder sb = new();

      sb.AppendLine("Course: " + Name);

      foreach (Assignment assignment in Assignments)
      {
        sb.AppendLine("\t" + assignment.ToString());
      }

      foreach (Module module in Modules)
      {
        sb.AppendLine(module.ToString());
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

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }
  }
}
