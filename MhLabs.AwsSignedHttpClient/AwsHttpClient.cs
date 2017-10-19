using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Newtonsoft.Json;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsHttpClient : HttpClient
    {
        public AwsHttpClient(ClientConfiguration config) : base(new AwsSignedHttpMessageHandler(RegionEndpoint.EUWest1))
        {
            BaseAddress = config.BaseUri;
        }

        public AwsHttpClient(RegionEndpoint region, string baseUri) : this(
            new ClientConfiguration {BaseUri = new Uri(baseUri), Region = region})
        {
        }

        public async Task<TReturn> SendAsync<TReturn>(HttpMethod method, string path, object postData = null, string contentType="application/json")
            where TReturn : class
        {
            path = path.TrimStart('/');
            using (var request = new HttpRequestMessage(method, BaseAddress + path))
            {
                if (method == HttpMethod.Post)
                {
                    var data = postData as HttpContent;
                    request.Content = data ?? new StringContent(postData is string ? postData.ToString() : JsonConvert.SerializeObject(postData), Encoding.UTF8,
                                          contentType);
                }
                var result = await SendAsync(request);
                if (!result.IsSuccessStatusCode)
                {
                    if (result.StatusCode == HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Unauthorized");
                }

                var response = await result.Content.ReadAsStringAsync();

                if (typeof(TReturn) == typeof(string))
                    return response as TReturn;
                return JsonConvert.DeserializeObject<TReturn>(response);
            }
        }
    }

    public class ClientConfiguration
    {
        public RegionEndpoint Region { get; set; }
        public Uri BaseUri { get; set; }
    }
}