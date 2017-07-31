using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace MhLabs.AwsSignedHttpClient.Credentials
{
    public static class CredentialProviderExtensions
    {
        private static readonly Regex _regionRegex =
            new Regex(@"\.([\w-]+)\.([\w-]+)\.amazonaws\.com$", RegexOptions.Compiled);
        //public static void Sign(this ICredentialsProvider credentialsProvider, System.Net.Http.HttpRequestMessage request, byte[] body)
        //    => credentialsProvider.Sign(new HttpRequestMessageAdapter(request), body);

        internal static void Sign(this ICredentialsProvider credentialsProvider, HttpRequestMessage request,
            byte[] body)
        {
            var credentials = credentialsProvider.GetCredentials();
            if (credentials == null)
                throw new Exception("Unable to retrieve credentials.");
            var regionService = ExtractRegionAndService(request.RequestUri);
            SignV4Util.SignRequest(request, body, credentials, regionService.Item1, regionService.Item2);
        }

        private static Tuple<string, string> ExtractRegionAndService(Uri url)
        {
            var match = _regionRegex.Match(url.Host);
            if (match.Success)
                return new Tuple<string, string>(match.Groups[1].Value, match.Groups[2].Value);
            throw new ArgumentException("Invalid url host. Expected something like ...us-east-1.es.amazonaws.com");
        }
    }
}