using System;
using System.Net.Http;
using MhLabs.AwsSignedHttpClient.Credentials;
using System.Threading.Tasks;

namespace MhLabs.AwsSignedHttpClient
{
    public static class HttpRequestMessageExtensions
    {
        internal static async Task SignRequest(this HttpRequestMessage request, ICredentialsProvider credentialsProvider, string region)
        {
            if (request == null)
                throw new ArgumentException("HttpRequestMessage is null");

            if (credentialsProvider == null)
                throw new Exception("CredentialsProvider is null. Probably because it is not registered in IoC");

            var body = VerbCanContainBody(request) ?
                await ExtractBody(request) :
                null;

            var credentials = credentialsProvider.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials required to sign the request.");

            SignV4Util.SignRequest(request, body, credentials, region, "execute-api");
        }

        private static async Task<byte[]> ExtractBody(HttpRequestMessage request)
        {
            if (request.Content == null) return new byte[0];

            return await request.Content?.ReadAsByteArrayAsync();
        }

        private static bool VerbCanContainBody(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete || request.Method == new HttpMethod("PATCH");
        }
    }
}
