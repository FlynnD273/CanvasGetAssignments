using CanvasApi.JsonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CanvasApi
{
    public class CanvasApiCaller
    {
        private static readonly HttpClient client = new();

        public string ApiKey { get; set; }

        public CanvasApiCaller (string key)
        {
            ApiKey = key;
        }

        public async Task<string> Call(string call)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new(@"https://canvas.wpi.edu/api/v1/" + call),
                Method = HttpMethod.Get,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            HttpResponseMessage result;
            try
            {
                result = await client.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                throw;
            }

            var content = await result.Content.ReadAsStringAsync();
            if (content.StartsWith("{\"errors\":"))
            {
                throw CanvasApiException.FromJson(content);
            }
            return content;
        }
    }
}
