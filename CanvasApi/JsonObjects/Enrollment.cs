using System.Text.Json.Serialization;

namespace CanvasApi.JsonObjects
{
  /**
{
  "id": 836397,
  "user_id": 36737,
  "course_id": 41587,
  "type": "StudentEnrollment",
  "created_at": "2022-02-14T13:20:07Z",
  "updated_at": "2022-09-21T14:29:58Z",
  "associated_user_id": null,
  "start_at": null,
  "end_at": null,
  "course_section_id": 7949,
  "root_account_id": 1,
  "limit_privileges_to_course_section": false,
  "temporary_enrollment_source_user_id": null,
  "temporary_enrollment_pairing_id": null,
  "enrollment_state": "active",
  "role": "StudentEnrollment",
  "role_id": 3,
  "last_activity_at": "2024-12-13T06:20:40Z",
  "last_attended_at": null,
  "total_activity_time": 0,
  "grades": {
    "html_url": "https://canvas.wpi.edu/courses/41587/grades/36737",
    "current_grade": null,
    "current_score": null,
    "final_grade": null,
    "final_score": null
  },
  "html_url": "https://canvas.wpi.edu/courses/41587/users/36737"
}
   */
  public class Enrollment : CanvasApi.JsonObjects.JsonObject
  {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("role_id")]
    public int RoleId { get; set; }
    [JsonPropertyName("course_id")]
    public int CourseId { get; set; }
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }
    [JsonPropertyName("grades")]
    public Grade Grades { get; set; }
  }
}
