using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MhLabs.AwsSignedHttpClient.Credentials;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsSignedHttpMessageHandler : BaseHttpMessageHandler
    {
        private readonly string _region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")?.ToLower();
        private readonly ICredentialsProvider _credentialsProvider;

        public AwsSignedHttpMessageHandler(ILoggerFactory loggerFactory, ICredentialsProvider credentialsProvider = null) : base(loggerFactory)
        {
            _credentialsProvider = credentialsProvider ?? CredentialChainProvider.Default;
        }

        public AwsSignedHttpMessageHandler(ICredentialsProvider credentialsProvider = null) : base(NullLoggerFactory.Instance)
        {
            _credentialsProvider = credentialsProvider ?? CredentialChainProvider.Default;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await request.SignRequest(_credentialsProvider, _region);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
