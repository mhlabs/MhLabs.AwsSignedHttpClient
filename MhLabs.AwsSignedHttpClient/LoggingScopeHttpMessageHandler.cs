using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MhLabs.AwsSignedHttpClient.Credentials;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsSignedHttpMessageHandlerWithLogging<TClient> : AwsSignedHttpMessageHandler
    {
        private readonly ILogger<TClient> _logger;

        public AwsSignedHttpMessageHandlerWithLogging(ILogger<TClient> logger, ICredentialsProvider credentialsProvider) : base(credentialsProvider)
        {
            _logger = logger ?? NullLogger<TClient>.Instance;
        }

        public AwsSignedHttpMessageHandlerWithLogging(ILogger<TClient> logger)
        {
            _logger = logger ?? NullLogger<TClient>.Instance;
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