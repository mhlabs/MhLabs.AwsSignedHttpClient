using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MhLabs.AwsSignedHttpClient.Credentials;
using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsSignedHttpMessageHandler<TClient>: BaseHttpMessageHandler<TClient>
    {
        private readonly string _region;
        private readonly ICredentialsProvider _credentialsProvider;
        private readonly ILogger _logger;

        public AwsSignedHttpMessageHandler(ILoggerFactory loggerFactory, ICredentialsProvider credentialsProvider = null) : base(loggerFactory)
        {
            _region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")?.ToLower();
            _credentialsProvider = credentialsProvider ?? CredentialChainProvider.Default;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SignRequest(request);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task SignRequest(HttpRequestMessage request)
        {
            var body = (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete || request.Method == new HttpMethod("PATCH")) ? await request.Content.ReadAsByteArrayAsync() : null;
            var credentials = _credentialsProvider.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials required to sign the request.");
                
            SignV4Util.SignRequest(request, body, credentials, _region, "execute-api");
        }
    }
}