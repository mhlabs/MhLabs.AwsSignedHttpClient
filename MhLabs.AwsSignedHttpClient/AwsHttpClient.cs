using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Newtonsoft.Json;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsHttpClient : HttpClient, IAmazonService, IAwsHttpClient
    {
        public IClientConfig Config => throw new NotImplementedException();

        public AwsHttpClient(string baseUrl = null) : base(new AwsSignedHttpMessageHandler(overrideSubSegmentNameFunc: message =>
             {
                 Console.WriteLine("PathAndQuery: " + message.RequestUri.PathAndQuery);
                 return message.RequestUri.PathAndQuery?.Split('/').FirstOrDefault(p => !string.IsNullOrEmpty(p));
             }))
        {
            BaseAddress = new Uri(baseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ?? Environment.GetEnvironmentVariable("ApiGatewayBaseUrl"));
        }

        public async Task<TReturn> SendAsync<TReturn>(HttpMethod method, string path, object postData = null,
            string contentType = "application/json")
            where TReturn : class
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, BaseAddress + path))
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

                var result = await this.SendAsync(request);

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