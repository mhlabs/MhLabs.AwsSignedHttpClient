using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MhLabs.AwsSignedHttpClient
{
    public class BaseHttpMessageHandler : DelegatingHandler
    {
        public string ImplementingName => this.GetType().Name;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"{ImplementingName} - {request.Method}");
            Console.WriteLine($"{ImplementingName} - {request.RequestUri}");

            if (!request.Headers.Contains(CorrelationHelper.CorrelationIdHeader))
            {
                request.Headers.Add(CorrelationHelper.CorrelationIdHeader, CorrelationHelper.CorrelationId ?? Guid.NewGuid().ToString());
            }

            var response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine($"{ImplementingName} - Response status code: {response?.StatusCode}");

            return response;
        }

    }
}