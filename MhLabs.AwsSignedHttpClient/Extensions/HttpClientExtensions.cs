using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Send request using httpClient
        /// </summary>
        /// <param name="client"></param>
        /// <param name="method"></param>
        /// <param name="path">with our without initial slash</param>
        /// <param name="postData">default null</param>
        /// <param name="contentType">default application/json</param>
        /// <param name="cancellationToken">default default :)</param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        public static async Task<HttpResponseMessage> MhSendAsync(this HttpClient client, HttpMethod method, string path, object postData = null,
            string contentType = "application/json", CancellationToken cancellationToken = default)
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, client.BaseAddress + path))
            {
                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete || method == new HttpMethod("PATCH"))
                {
                    request.Content = ToContent(postData, contentType, postData as HttpContent);
                }
                
                var response = await client.SendAsync(request, cancellationToken);
                return response;
            }
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
                    request.Content = ToContent(postData, contentType, postData as HttpContent);
                }

                var response = await client.SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync();

                return CreateExecutionResult<TReturn>(response, content);
            }
        }

        public static ExecutionResult<TReturn> CreateExecutionResult<TReturn>(HttpResponseMessage response, string content) where TReturn : class
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ExecutionResult<TReturn>(HttpStatusCode.Forbidden, default(TReturn), content);
            }

            var data = ToData<TReturn>(content);
            return new ExecutionResult<TReturn>(response.StatusCode, data, content);
        }

        public static HttpContent ToContent(object postData, string contentType, HttpContent requestData)
        {
            var content = postData is string ?
                                postData.ToString() :
                                JsonConvert.SerializeObject(postData);

            return requestData ?? new StringContent(content, Encoding.UTF8, contentType);
        }

        public static TReturn ToData<TReturn>(string content) where TReturn : class
        {
            if (string.IsNullOrWhiteSpace(content)) return default(TReturn);

            if (typeof(TReturn) == typeof(string) || typeof(TReturn) == typeof(decimal))
            {
                return content as TReturn;
            }

            TReturn result = default(TReturn);

            try
            {
                result = JsonConvert.DeserializeObject<TReturn>(content);
            }
            catch { }

            return result;
        }
    }
}