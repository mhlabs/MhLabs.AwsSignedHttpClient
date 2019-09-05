using System;

namespace MhLabs.AwsSignedHttpClient
{
    public class BaseUrlMissingException : Exception
    {
        public BaseUrlMissingException(): base("Please provide a valid base url using HttpOptions.BaseUrl or the environment variable key 'ApiBaseUrl'")
        {
            
        }
    }
}
