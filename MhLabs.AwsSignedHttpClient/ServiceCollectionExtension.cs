using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Http;
using System.Linq;
using System.Threading;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ServiceCollectionExtension
    {
        private static readonly Random _jitterer = new Random();

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, string baseUrl = null, bool useCircuitBreaker = true,
            RetryLevel retryLevel = RetryLevel.Read) where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler>();

            var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
                {
                    client.BaseAddress = new Uri(baseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ?? Environment.GetEnvironmentVariable("ApiGatewayBaseUrl"));
                }).AddHttpMessageHandler<AwsSignedHttpMessageHandler>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (useCircuitBreaker)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetCircuitBreakerPolicy());
            }

            if (retryLevel == RetryLevel.Update)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetRetryPolicy());
            }

            if (retryLevel == RetryLevel.Read)
            {
                httpClientBuilder
                    .AddPolicyHandler(request =>
                        request.Method == HttpMethod.Get
                            ? GetRetryPolicy()
                            : GetNoRetryPolicy());
            }

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30), (resp, ts) =>
                {

                },
                () => { });
        }
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3,
                            retryAttempt =>
                            {
                                return TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                            });

            return retryPolicy;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetNoRetryPolicy()
        {
            var noOpPolicy = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
            return noOpPolicy;
        }

    }
}