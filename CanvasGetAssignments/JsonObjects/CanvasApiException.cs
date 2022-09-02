using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CanvasGetAssignments.JsonObjects
{
    internal class CanvasApiException : Exception
    {
        private CanvasApiException () { }
        private CanvasApiException (string message) : base(message) { }

        public static CanvasApiException FromJson(string errorMessage)
        {
            ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(errorMessage);
            return new CanvasApiException(string.Join("\n", (IEnumerable<Error>)error.Errors));
        }
    }
}
