using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{
    public class BaseHttpMessageHandler : DelegatingHandler
    {
        public string ImplementingName => this.GetType().Name;

        private readonly ILogger<BaseHttpMessageHandler> _logger;

        public BaseHttpMessageHandler(ILogger<BaseHttpMessageHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("{MessageHandler} - {Method}: {RequestUri}", ImplementingName, request.Method, request.RequestUri);

            if (!request.Headers.Contains(CorrelationHelper.CorrelationIdHeader))
            {
                request.Headers.Add(CorrelationHelper.CorrelationIdHeader, CorrelationHelper.CorrelationId ?? Guid.NewGuid().ToString());
            }

            var response = await base.SendAsync(request, cancellationToken);

            _logger.LogInformation("{MessageHandler} - Response status code: {StatusCode}", ImplementingName, response?.StatusCode);
            return response;
        }

    }
}