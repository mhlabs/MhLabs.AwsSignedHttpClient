namespace MhLabs.AwsSignedHttpClient.MessageHandlers
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class TracingMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            if (!httpRequestMessage.Headers.Contains(CorrelationHelper.CorrelationIdHeader))
            {
                httpRequestMessage.Headers.Add(CorrelationHelper.CorrelationIdHeader, CorrelationHelper.CorrelationId ?? Guid.NewGuid().ToString());
            }

            var xRayEnvVar = Environment.GetEnvironmentVariable("_X_AMZN_TRACE_ID");
            if (!string.IsNullOrWhiteSpace(xRayEnvVar))
            {
                httpRequestMessage.Headers.Add("X-Amzn-Trace-Id", xRayEnvVar);
            }

            return await base.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}
