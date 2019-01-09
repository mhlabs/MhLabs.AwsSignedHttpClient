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

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, string baseUrl = null, bool useCircuitBreaker = true) where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler>();

            var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
                {
                    client.BaseAddress = new Uri(baseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ?? Environment.GetEnvironmentVariable("ApiGatewayBaseUrl"));
                });

            if (useCircuitBreaker)
            {
                httpClientBuilder.AddHttpMessageHandler<AwsSignedHttpMessageHandler>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
            }
            return services;
        }

        static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30), (resp, ts) =>
                {
                    
                },
                () => { });
        }
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3,
                            retryAttempt =>
                            {
                                return TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt))
                             + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                            });
        }
    }
}