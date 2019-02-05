namespace MhLabs.AwsSignedHttpClient
{
    public class HttpOptions
    {
        public string BaseUrl { get; set; } = null;
        public bool UseCircuitBreaker { get; set; } = true;
        public RetryLevel RetryLevel { get; set; } = RetryLevel.Read;
    }
}