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

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            if (options == null) options = new HttpOptions();

            services.AddTransient<AwsSignedHttpMessageHandler>();

            var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
                {
                    client.BaseAddress = new Uri(options.BaseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ?? Environment.GetEnvironmentVariable("ApiGatewayBaseUrl"));
                }).AddHttpMessageHandler<AwsSignedHttpMessageHandler>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (options.RetryLevel == RetryLevel.Update)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetRetryPolicy());
            }

            if (options.RetryLevel == RetryLevel.Read)
            {
                httpClientBuilder
                    .AddPolicyHandler(request =>
                        request.Method == HttpMethod.Get
                            ? GetRetryPolicy()
                            : GetNoRetryPolicy());
            }

            if (options.UseCircuitBreaker)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetCircuitBreakerPolicy());
            }

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30), (resp, ts) =>
                {

                },
                () => { });

            return circuitBreakerPolicy;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3,
                            retryAttempt =>
                            {
                                var delay = TimeSpan.FromMilliseconds(Math.Pow(5, retryAttempt))
                                    + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));

                                Console.WriteLine($"AwsSignedHttpClient - Retrying call, attempt: {retryAttempt}, delay ms: {delay.TotalMilliseconds}");

                                return delay;
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