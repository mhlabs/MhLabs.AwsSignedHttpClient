using System.Threading;
using Amazon.Lambda.Core;

namespace MhLabs.AwsSignedHttpClient
{
    public static class LambdaContextExtension
    {
        public static CancellationToken GetCancellationToken(this ILambdaContext lambdaContext, int? timeoutMilliseconds = null) {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMilliseconds ?? (int)(lambdaContext.RemainingTime.TotalMilliseconds * 0.75));
            return cts.Token;
        }
    }
}