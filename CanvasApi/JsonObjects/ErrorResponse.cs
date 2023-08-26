﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
		public class ErrorResponse : CanvasApi.JsonObjects.JsonObject
		{
				[JsonPropertyName("errors")]
				public Error[] Errors { get; set; }
		}
}
