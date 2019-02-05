using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MhLabs.AwsSignedHttpClient
{
    public static class HttpResponseMessageExtension
    {

        private static Uri _baseAddress = new Uri(Environment.GetEnvironmentVariable("ApiBaseUrl") ?? Environment.GetEnvironmentVariable("ApiGatewayBaseUrl"));

        public static async Task<TReturn> SendAsync<TReturn>(this HttpClient client, HttpMethod method, string path, object postData = null,
            string contentType = "application/json", CancellationToken cancellationToken = default(CancellationToken))
            where TReturn : class
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, _baseAddress + path))
            {
                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete)
                {
                    var data = postData as HttpContent;
                    request.Content = data ?? new StringContent(
                                          postData is string
                                              ? postData.ToString()
                                              : JsonConvert.SerializeObject(postData), Encoding.UTF8,
                                          contentType);
                }

                var result = await client.SendAsync(request, cancellationToken);

                var response = await result.Content.ReadAsStringAsync();

                if (!result.IsSuccessStatusCode)
                {
                    Console.WriteLine("result.StatusCode: " + result.StatusCode);
                    Console.WriteLine("result.Content: " + response);

                    if (result.StatusCode == HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Unauthorized");
                }

                if (typeof(TReturn) == typeof(string))
                    return response as TReturn;
                return JsonConvert.DeserializeObject<TReturn>(response);
            }
        }
    }
}