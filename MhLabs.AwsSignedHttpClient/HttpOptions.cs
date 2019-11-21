using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public class HttpOptions
    {
        public string BaseUrl { get; set; } = null;
        public bool UseCircuitBreaker { get; set; } = false;
        public RetryLevel RetryLevel { get; set; } = RetryLevel.Read;
        public ILogger logger { get; set; } = NullLogger.Instance;
    }
}