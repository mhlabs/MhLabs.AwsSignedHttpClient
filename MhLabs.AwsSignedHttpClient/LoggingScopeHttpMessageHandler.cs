using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{

    public class LoggingScopeHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public LoggingScopeHttpMessageHandler(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return await SendAsyncWithLogging(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsyncWithLogging(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            timer.Start();

            _logger.LogInformation("Request - {Method}: {Uri}", request?.Method, request?.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);

            timer.Stop();
            _logger.LogInformation("Response - {Method}: {Uri} - {StatusCode} - {Elapsed} ms - {IsSuccessStatusCode}", request?.Method, request?.RequestUri, response.StatusCode, timer.ElapsedMilliseconds, response.IsSuccessStatusCode);

            return response;
        }
    }
}