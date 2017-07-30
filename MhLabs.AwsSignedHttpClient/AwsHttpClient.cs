using Amazon;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace signed_request_test.Http
{

    public class AwsHttpClient : HttpClient
    {
        public AwsHttpClient(ClientConfiguration config) : base(new AwsSignedHttpMessageHandler(RegionEndpoint.EUWest1))
        {
            BaseAddress = config.BaseUri;
        }

        public AwsHttpClient(RegionEndpoint region, string baseUri) : this(new ClientConfiguration {BaseUri = new Uri(baseUri), Region = region})
        {
        }

        public async Task<TReturn> SendAsync<TReturn>(HttpMethod method, string requestUri, object postData = null) where TReturn : class
        {
            using (var request = new HttpRequestMessage(method, BaseAddress + requestUri))
            {
                if (method == HttpMethod.Post)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8);
                }
                var result = await SendAsync(request);
                var response = await result.Content.ReadAsStringAsync();
                
                if (typeof(TReturn) == typeof(string))
                {
                    return response as TReturn;
                }
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
