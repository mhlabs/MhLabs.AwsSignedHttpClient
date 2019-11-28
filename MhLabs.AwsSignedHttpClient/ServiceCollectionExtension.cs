using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ServiceCollectionExtension
    {
        private static readonly Random _jitterer = new Random();

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler<TClient>>();

            return AddMhHttpClient<TClient, TImplementation>(services, options);
        }

        public static IServiceCollection AddUnsignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            return AddMhHttpClient<TClient, TImplementation>(services, options);
        }

        private static IServiceCollection AddMhHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            
            if (options == null) options = new HttpOptions();

            var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
            {
                client.BaseAddress = GetBaseUrl(options);
            }).AddHttpMessageHandler<AwsSignedHttpMessageHandler<TClient>>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (options.RetryLevel == RetryLevel.Update)
            {
                httpClientBuilder
                    .AddPolicyHandler((serviceCollection, request) => GetRetryPolicy<TClient>(serviceCollection));
            }

            if (options.RetryLevel == RetryLevel.Read)
            {
                httpClientBuilder
                    .AddPolicyHandler((serviceCollection, request) => 
                        request.Method == HttpMethod.Get
                            ? GetRetryPolicy<TClient>(serviceCollection)
                            : GetNoRetryPolicy());
            }

            if (options.UseCircuitBreaker)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetCircuitBreakerPolicy());
            }

            return services;
        }

        private static Uri GetBaseUrl(HttpOptions options)
        {
            var baseUrl = options.BaseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ??
                          Environment.GetEnvironmentVariable("ApiGatewayBaseUrl");

            if (baseUrl == null)
                throw new BaseUrlMissingException();
            
            return new Uri(baseUrl);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrInner<IOException>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30), (resp, ts) =>
                {

                },
                () => { });

            return circuitBreakerPolicy;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<TClient>(IServiceProvider serviceProvider)
        {

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrInner<IOException>()
                .WaitAndRetryAsync(3,
                            retryAttempt =>
                            {
                                return TimeSpan.FromMilliseconds(Math.Pow(5, retryAttempt)) + 
                                       TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                            },
                            onRetry: (outcome, timespan, retryAttempt, context) =>
                            {
                                var logger = serviceProvider.GetService<ILogger<TClient>>() ?? NullLogger<TClient>.Instance;
                                logger.LogWarning("Delaying for {Delay} ms, then making retry {Retry}. StatusCode: {StatusCode}. Exception: {Exception}", 
                                    timespan.TotalMilliseconds, retryAttempt, outcome?.Result?.ReasonPhrase, outcome?.Result?.StatusCode, outcome?.Exception?.ToString());
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