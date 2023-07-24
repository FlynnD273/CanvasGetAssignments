using System.Text.Json;

namespace CanvasApi.JsonObjects
{
    public class CanvasApiException : Exception
    {
        private CanvasApiException() { }
        private CanvasApiException(string message) : base(message) { }

        public static CanvasApiException FromJson(string errorMessage)
        {
						if (string.IsNullOrEmpty(errorMessage))
						{
								return new CanvasApiException("JSON string was empty");
						}

            try
            {
                ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(errorMessage);
                if (error == null)
                {
                    return new CanvasApiException($"No error given in JSON {errorMessage}");
                }
                return new CanvasApiException(string.Join("\n", (IEnumerable<Error>)error.Errors));
            }
            catch (JsonException e)
            {
                return new CanvasApiException(e.Message);
            }
        }
    }
}
