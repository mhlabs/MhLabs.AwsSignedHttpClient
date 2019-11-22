using Amazon.Runtime;
using MhLabs.AwsSignedHttpClient.Credentials;

namespace MhLabs.AwsSignedHttpClient
{

    public class AwsFileSignedHttpMessageHandler : AwsSignedHttpMessageHandler
    {
        public AwsFileSignedHttpMessageHandler() 
            : base(new AwsCredentialsFileProvider())
        {
        }
    }
}