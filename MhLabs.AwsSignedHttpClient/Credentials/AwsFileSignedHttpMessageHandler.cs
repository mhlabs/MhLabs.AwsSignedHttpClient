using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{

    public class AwsFileSignedHttpMessageHandler<TClient> : AwsSignedHttpMessageHandler<TClient>
    {
        public AwsFileSignedHttpMessageHandler(ILoggerFactory loggerFactory) 
            : base(loggerFactory, new AwsCredentialsFileProvider())
        {
        }
    }
}