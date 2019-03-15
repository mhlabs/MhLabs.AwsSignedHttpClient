using System;
using System.Net.Http;
using MhLabs.Extensions;

namespace MhLabs.AwsSignedHttpClient
{
    [Obsolete("Use HttpClient instead")]
    public class AwsHttpClient : HttpClient
    {
        public AwsHttpClient() : base(new AwsSignedHttpMessageHandler { InnerHandler = new HttpClientHandler() }) => BaseAddress = (Environment.GetEnvironmentVariable("ApiGatewayBaseUrl") ?? Environment.GetEnvironmentVariable("ApiBaseUrl")).ToUri();
    }
}