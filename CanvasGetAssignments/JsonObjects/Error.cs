using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasGetAssignments.JsonObjects
{
    internal class Error
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
