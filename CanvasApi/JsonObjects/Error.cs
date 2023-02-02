using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasApi.JsonObjects
{
    public class Error
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        public override string ToString() => Message;
    }
}
