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

            if (executionResult.StatusCode == HttpStatusCode.Forbidden)
            {
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
                    request.Content = ToContent(postData, contentType, request, postData as HttpContent);
                }

                var response = await client.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new ExecutionResult<TReturn>(HttpStatusCode.Forbidden, default(TReturn));
                }

                var data = ToData<TReturn>(content);
                return new ExecutionResult<TReturn>(response.StatusCode, data);
            }
        }

        private static HttpContent ToContent(object postData, string contentType, HttpRequestMessage request, HttpContent requestData)
        {
            var content = postData is string ? 
                                postData.ToString() : 
                                JsonConvert.SerializeObject(postData);

            return requestData ?? new StringContent(content, Encoding.UTF8, contentType);
        }

        private static TReturn ToData<TReturn>(string content) where TReturn : class
        {
            if (typeof(TReturn) == typeof(string) || typeof(TReturn) == typeof(decimal))
            {
                return content as TReturn;
            }
            return JsonConvert.DeserializeObject<TReturn>(content);
        }
    }
}