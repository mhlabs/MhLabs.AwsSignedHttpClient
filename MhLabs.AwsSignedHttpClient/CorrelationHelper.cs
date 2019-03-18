namespace MhLabs.AwsSignedHttpClient
{
    public static class CorrelationHelper
    {
        public const string CorrelationIdHeader = "mh-correlation-id";
        public static string CorrelationId { get; set; } = null;
    }
}