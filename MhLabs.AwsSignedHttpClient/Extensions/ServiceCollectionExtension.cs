using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ServiceCollectionExtension
    {
        private static readonly Random _jitterer = new Random();

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler<TClient>>();
            return AddMhHttpClient<TClient, TImplementation, AwsSignedHttpMessageHandler<TClient>>(services, options);
        }

        public static IServiceCollection AddUnsignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<BaseHttpMessageHandler<TClient>>();
            return AddMhHttpClient<TClient, TImplementation, BaseHttpMessageHandler<TClient>>(services, options);
        }

        private static IServiceCollection AddMhHttpClient<TClient, TImplementation, TMessageHandler>(this IServiceCollection services, HttpOptions options = null) where TClient : class
            where TImplementation : class, TClient
            where TMessageHandler : DelegatingHandler
        {
            options = options ?? new HttpOptions();
            
            var builder = CreateHttpBuilder<TClient, TImplementation, TMessageHandler>(services, options);

            ApplyPollyConfiguration<TClient, TMessageHandler>(options, builder);

            return services;
        }

        private static IHttpClientBuilder CreateHttpBuilder<TClient, TImplementation, TMessageHandler>(IServiceCollection services, HttpOptions options)
            where TClient : class
            where TImplementation : class, TClient
            where TMessageHandler : DelegatingHandler
        {
            return services.AddHttpClient<TClient, TImplementation>(client =>
            {
                client.BaseAddress = GetBaseUrl(options);
            }).AddHttpMessageHandler<TMessageHandler>()
                            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        }

        private static void ApplyPollyConfiguration<TClient, TMessageHandler>(HttpOptions options, IHttpClientBuilder httpClientBuilder) where TClient : class
        {
            if (options.RetryLevel == RetryLevel.Update)
            {
                httpClientBuilder
                    .AddPolicyHandler((serviceCollection, request) => GetRetryPolicy<TClient, TMessageHandler>(serviceCollection));
            }

            if (options.RetryLevel == RetryLevel.Read)
            {
                httpClientBuilder
                    .AddPolicyHandler((serviceCollection, request) =>
                        request.Method == HttpMethod.Get
                            ? GetRetryPolicy<TClient, TMessageHandler>(serviceCollection)
                            : GetNoRetryPolicy());
            }

            if (options.UseCircuitBreaker)
            {
                httpClientBuilder.AddPolicyHandler((serviceCollection, request) => GetCircuitBreakerPolicy<TClient, TMessageHandler>(serviceCollection));
            }
        }

        private static Uri GetBaseUrl(HttpOptions options)
        {
            var baseUrl = options.BaseUrl ?? Environment.GetEnvironmentVariable("ApiBaseUrl") ??
                          Environment.GetEnvironmentVariable("ApiGatewayBaseUrl");

            if (baseUrl == null)
                throw new BaseUrlMissingException();
            
            return new Uri(baseUrl);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy<TClient, TMessageHandler>(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateDefaultLogger<TClient, TMessageHandler>();

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrInner<IOException>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30), 
                (o, ts) =>
                {
                    logger.LogWarning("Circuit breaker - State: {CircuitState}. Event: {CircuitEvent}.", Polly.CircuitBreaker.CircuitState.Open, "onBreak");
                    logger.LogWarning("Response: {Delay}ms. Reason: {ReasonPhrase}. StatusCode: {StatusCode}. Exception: {Exception}", 
                        ts.TotalMilliseconds, o?.Result?.ReasonPhrase, o?.Result?.StatusCode, o?.Exception?.ToString());
                },
                () => logger.LogWarning("Circuit breaker - State: {CircuitState}. Event: {CircuitEvent}.", Polly.CircuitBreaker.CircuitState.Closed, "onReset"),
                () => logger.LogWarning("Circuit breaker - State: {CircuitState}. Event: {CircuitEvent}.", Polly.CircuitBreaker.CircuitState.HalfOpen, "onHalfOpen"));

            return circuitBreakerPolicy;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<TClient, TMessageHandler>(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateDefaultLogger<TClient, TMessageHandler>();

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
                                logger.LogWarning("Delaying for {Delay} ms, then making retry {Retry}. StatusCode: {StatusCode}. Exception: {Exception}", 
                                    timespan.TotalMilliseconds, retryAttempt, outcome?.Result?.ReasonPhrase, outcome?.Result?.StatusCode, outcome?.Exception?.ToString());
                            });

            return retryPolicy;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetNoRetryPolicy()
        {
            return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
        }

    }
}