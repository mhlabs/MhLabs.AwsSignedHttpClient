using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MhLabs.AwsSignedHttpClient
{
    public static class HttpClientExtensions
    {

        public static async Task<TReturn> SendAsync<TReturn>(this HttpClient client, HttpMethod method, string path, object postData = null,
            string contentType = "application/json", CancellationToken cancellationToken = default(CancellationToken))
            where TReturn : class
        {
            var executionResult = await ExecuteAsync<TReturn>(client, method, path, postData, contentType, cancellationToken);

            if (!executionResult.Success)
            {
                if (executionResult.StatusCode == HttpStatusCode.Forbidden)
                    throw new UnauthorizedAccessException("Unauthorized");
            }

            return executionResult.Data;
        }


        public static async Task<ExecutionResult<TReturn>> ExecuteAsync<TReturn>(this HttpClient client, HttpMethod method, string path, object postData = null,
            string contentType = "application/json", CancellationToken cancellationToken = default(CancellationToken))
            where TReturn : class
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, client.BaseAddress + path))
            {
                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete || method == new HttpMethod("PATCH"))
                {
                    var requestData = postData as HttpContent;
                    request.Content = requestData ?? new StringContent(
                                          postData is string
                                              ? postData.ToString()
                                              : JsonConvert.SerializeObject(postData), Encoding.UTF8,
                                          contentType);
                }

                var response = await client.SendAsync(request, cancellationToken);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("result.StatusCode: " + response.StatusCode);
                    Console.WriteLine("result.Content: " + content);

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        return new ExecutionResult<TReturn>(HttpStatusCode.Forbidden, default(TReturn));
                    }
                }

                TReturn data;

                if (typeof(TReturn) == typeof(string) || typeof(TReturn) == typeof(decimal))
                {
                    data = content as TReturn;
                }
                else
                {
                    data = JsonConvert.DeserializeObject<TReturn>(content);
                }

                return new ExecutionResult<TReturn>(response.StatusCode, data);

            }
        }

    }
}