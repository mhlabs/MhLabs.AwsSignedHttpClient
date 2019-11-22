using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;

namespace MhLabs.AwsSignedHttpClient
{
    public class BaseHttpMessageHandler : DelegatingHandler
    {
        public string ImplementingName => this.GetType().Name;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains(CorrelationHelper.CorrelationIdHeader))
            {
                request.Headers.Add(CorrelationHelper.CorrelationIdHeader, CorrelationHelper.CorrelationId ?? Guid.NewGuid().ToString());
            }

            var response = await base.SendAsync(request, cancellationToken);

            return response;
        }
    }
}