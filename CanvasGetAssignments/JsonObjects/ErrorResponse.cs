using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasGetAssignments.JsonObjects
{
    internal class ErrorResponse
    {
        [JsonPropertyName("errors")]
        public Error[] Errors { get; set; }
    }
}
