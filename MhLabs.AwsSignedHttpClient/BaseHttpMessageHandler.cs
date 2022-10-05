using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{
    public class BaseHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public BaseHttpMessageHandler (ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateDefaultLogger(GetType().Name);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains(CorrelationHelper.CorrelationIdHeader))
            {
                request.Headers.Add(CorrelationHelper.CorrelationIdHeader, CorrelationHelper.CorrelationId ?? Guid.NewGuid().ToString());
                
                var xRayEnvVar = Environment.GetEnvironmentVariable("_X_AMZN_TRACE_ID");
                if (!string.IsNullOrWhiteSpace(xRayEnvVar))
                    request.Headers.Add("X-Amzn-Trace-Id", xRayEnvVar);
                
            }

            var timer = new Stopwatch();
            timer.Start();

            _logger.LogInformation("HttpRequest - {Method}: {Uri}", request?.Method, request?.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);

            timer.Stop();
            _logger.LogInformation("HttpResponse - {Method}: {Uri} returned {StatusCode} in {Elapsed}ms. Success: {IsSuccessStatusCode}", request?.Method, request?.RequestUri, response.StatusCode, timer.ElapsedMilliseconds, response.IsSuccessStatusCode);

            return response;
        }
    }
}
