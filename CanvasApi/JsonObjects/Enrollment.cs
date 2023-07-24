using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
		public class Enrollment
		{
				[JsonPropertyName("type")]
				public string Type { get; set; }
				[JsonPropertyName("role")]
				public string Role { get; set; }
				[JsonPropertyName("role_id")]
				public int RoleId { get; set; }
				[JsonPropertyName("user_id")]
				public int UserId { get; set; }
		}
}
