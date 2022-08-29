using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CanvasGetAssignments
{
    internal class CanvasApiException : Exception
    {
        public ErrorResponse Error { get; }

        public CanvasApiException(string errorMessage)
        {
            Error = JsonSerializer.Deserialize<ErrorResponse>(errorMessage);
        }
    }
}
