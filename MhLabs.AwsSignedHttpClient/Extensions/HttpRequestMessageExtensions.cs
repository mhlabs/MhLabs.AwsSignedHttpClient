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
            var body = (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete || request.Method == new HttpMethod("PATCH")) ? await request.Content.ReadAsByteArrayAsync() : null;
            var credentials = credentialsProvider.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials required to sign the request.");

            SignV4Util.SignRequest(request, body, credentials, region, "execute-api");
        }
    }
}
