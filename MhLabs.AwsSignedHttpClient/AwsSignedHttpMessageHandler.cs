using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using signed_request_test.Http.Credentials;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsSignedHttpMessageHandler : HttpClientHandler
    {
        private readonly ICredentialsProvider _credentialsProvider;
        private readonly string _region;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await SignRequest(request);
            return await base.SendAsync(request, cancellationToken);
        }

 
        public AwsSignedHttpMessageHandler(RegionEndpoint region)
            : this(region, CredentialChainProvider.Default)
        {
        }

        public AwsSignedHttpMessageHandler(RegionEndpoint region, ICredentialsProvider credentialsProvider)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            _region = region.SystemName.ToLowerInvariant();
            _credentialsProvider = credentialsProvider ?? throw new ArgumentNullException(nameof(credentialsProvider));
        }


        private async Task SignRequest(HttpRequestMessage request)
        {

            var body = request.Method == HttpMethod.Post ? await request.Content.ReadAsByteArrayAsync() : null;
            var credentials = _credentialsProvider.GetCredentials();
            if (credentials == null)
            {
                throw new Exception("Unable to retrieve credentials required to sign the request.");
            }
            SignV4Util.SignRequest(request, body, credentials, _region, "execute-api");
        }
    }
}
