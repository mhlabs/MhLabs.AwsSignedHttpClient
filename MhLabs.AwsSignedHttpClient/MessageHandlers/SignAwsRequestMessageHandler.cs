namespace MhLabs.AwsSignedHttpClient.MessageHandlers
{
    using System;
    using MhLabs.AwsSignedHttpClient.Credentials;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;
    using Aws.Crt.Auth;
    using Aws.Crt.Http;
    using System.IO;

    public class SignAwsRequestMessageHandler : DelegatingHandler
    {
        private static readonly string Region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")?.ToLower();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            await SignRequest(httpRequestMessage);
            return await base.SendAsync(httpRequestMessage, cancellationToken);
        }

        internal static async Task SignRequest(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentException("HttpRequestMessage is null");

            var body = VerbCanContainBody(request) ?
                await ExtractBody(request) :
                null;

            var credentials = CredentialChainProvider.Default.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials required to sign the request.");

            SignAwsRequest(request, body, credentials, Region, "execute-api");
        }

        private static async Task<byte[]> ExtractBody(HttpRequestMessage request)
        {
            if (request.Content == null) return Array.Empty<byte>();

            return await request.Content?.ReadAsByteArrayAsync()!;
        }

        private static bool VerbCanContainBody(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete || request.Method == new HttpMethod("PATCH");
        }

        private static void SignAwsRequest(HttpRequestMessage request, byte[] body, AwsCredentials credentials, string region, string service)
        {
            var config = new AwsSigningConfig
            {
                Service = service,
                Region = region,
                Algorithm = AwsSigningAlgorithm.SIGV4A,
                SignatureType = AwsSignatureType.HTTP_REQUEST_VIA_HEADERS,
                SignedBodyHeader = AwsSignedBodyHeaderType.X_AMZ_CONTENT_SHA256,
                Credentials = new Credentials(credentials.AccessKey, credentials.SecretKey, credentials.Token),
            };

            var signHttpRequest = new HttpRequest
            {
                Method = request.Method.Method,
                Uri = request.RequestUri.PathAndQuery,
                Headers = new[] { new HttpHeader("host", request.RequestUri.Host) },
                BodyStream = request.Content?.ReadAsStream() ?? Stream.Null
            };

            var result = AwsSigner.SignHttpRequest(signHttpRequest, config);
            var signingResult = result.Get();
            var signedRequest = signingResult.SignedRequest;

            var headers = signedRequest.Headers;

            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }
    }

}
