using Amazon.Runtime;
using MhLabs.AwsSignedHttpClient.Credentials;

namespace MhLabs.AwsSignedHttpClient
{
    public class AwsCredentialsFileProvider : ICredentialsProvider
    {
        public AwsCredentials GetCredentials()
        {
            var credentials = FallbackCredentialsFactory.GetCredentials().GetCredentials();
            return new AwsCredentials
            {
                Token = credentials.Token,
                AccessKey = credentials.AccessKey,
                SecretKey = credentials.SecretKey
            };
        }
    }
}