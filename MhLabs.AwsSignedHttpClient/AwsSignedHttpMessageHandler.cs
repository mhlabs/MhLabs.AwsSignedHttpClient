using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using MhLabs.AwsSignedHttpClient.Credentials;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsSignedHttpMessageHandler<TClient> : BaseHttpMessageHandler
    {
        private readonly string _region;
        private readonly ICredentialsProvider _credentialsProvider;
        private readonly ILogger _logger;

        public AwsSignedHttpMessageHandler(ILoggerFactory loggerFactory, ICredentialsProvider credentialsProvider = null)
        {
            System.Console.WriteLine($"[>]: Creating with ILoggerFactory: {loggerFactory?.GetType()}");
            _logger = loggerFactory.CreateDefaultLogger<TClient>();
            _region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")?.ToLower();
            _credentialsProvider = credentialsProvider ?? CredentialChainProvider.Default;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SignRequest(request);

            return await SendAsyncWithLogging(request, cancellationToken);
        }

        private async Task SignRequest(HttpRequestMessage request)
        {
            var body = (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete || request.Method == new HttpMethod("PATCH")) ? await request.Content.ReadAsByteArrayAsync() : null;
            var credentials = _credentialsProvider.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials required to sign the request.");
            SignV4Util.SignRequest(request, body, credentials, _region, "execute-api");
        }

        private async Task<HttpResponseMessage> SendAsyncWithLogging(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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