using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using MhLabs.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace MhLabs.AwsSignedHttpClient
{
    [Obsolete("Use HttpClient instead")]

    public class AwsHttpClient : HttpClient, IAmazonService
    {
        public IClientConfig Config => throw new NotImplementedException();

        public AwsHttpClient(string baseUrl) : base(new AwsSignedHttpMessageHandler () { InnerHandler = new HttpClientHandler() })
        {
            BaseAddress = baseUrl.ToUri();
        }

        public async Task<TReturn> SendAsync<TReturn>(HttpMethod method, string path, object postData = null,
            string contentType = "application/json")
            where TReturn : class
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, BaseAddress + path))
            {
                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete || method == new HttpMethod("PATCH"))
                {
                    var data = postData as HttpContent;
                    request.Content = data ?? new StringContent(
                                          postData is string
                                              ? postData.ToString()
                                              : JsonConvert.SerializeObject(postData), Encoding.UTF8,
                                          contentType);
                }

                var result = await this.SendAsync(request);

                var response = await result.Content.ReadAsStringAsync();

                if (!result.IsSuccessStatusCode)
                {
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