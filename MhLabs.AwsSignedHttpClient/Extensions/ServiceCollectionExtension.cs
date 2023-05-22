using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using MhLabs.AwsSignedHttpClient.MessageHandlers;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ServiceCollectionExtension
    {
        private static readonly Random _jitterer = new Random();

        public static IServiceCollection AddSignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) 
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler>();
            return AddMhHttpClient<TClient, TImplementation, AwsSignedHttpMessageHandler>(services, options);

        }
        /// <summary>
        /// AddSignedHttpClientWitHttpClientBuilder
        /// This method adds a creates a HttpClient with AwsSignedHttpMessageHandler and returns IHttpClientBuilder
        /// which can be used to configure the client further
        /// </summary>
        /// <typeparam name="TClient"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddSignedHttpClientWitHttpClientBuilder<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<AwsSignedHttpMessageHandler>();
            return AddMhHttpClientWitHttpClientBuilder<TClient, TImplementation, AwsSignedHttpMessageHandler>(services, options);

        }
        /// <summary>
        /// AddMhHttpClient
        /// This method adds and creates a HttpClient with only TracingMessageHandler and returns IHttpClientBuilder
        /// Signing Aws requests will require SignAwsRequestMessageHandler to be added additionally to the client
        /// SignAwsRequestMessageHandler is already registered in the container and can be added as a message handler
        /// </summary>
        /// <typeparam name="TClient"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddMhHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null)
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<SignAwsRequestMessageHandler>();
            services.AddTransient<TracingMessageHandler>();
            return AddMhHttpClientWitHttpClientBuilder<TClient, TImplementation, TracingMessageHandler>(services, options);
        }

        public static IServiceCollection AddUnsignedHttpClient<TClient, TImplementation>(this IServiceCollection services, HttpOptions options = null) 
            where TClient : class
            where TImplementation : class, TClient
        {
            services.AddTransient<BaseHttpMessageHandler>();
            return AddMhHttpClient<TClient, TImplementation, BaseHttpMessageHandler>(services, options);
        }

        private static IHttpClientBuilder AddMhHttpClientWitHttpClientBuilder<TClient, TImplementation, TMessageHandler>(this IServiceCollection services, HttpOptions options = null)
            where TClient : class
            where TImplementation : class, TClient
            where TMessageHandler : DelegatingHandler
        {
            options = options ?? new HttpOptions();

            var builder = CreateHttpBuilder<TClient, TImplementation, TMessageHandler>(services, options);

            ApplyPollyConfiguration<TClient, TMessageHandler>(options, builder);

            return builder;
        }

        private static IServiceCollection AddMhHttpClient<TClient, TImplementation, TMessageHandler>(this IServiceCollection services, HttpOptions options = null) 
            where TClient : class
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

            return HttpPolicyExtensions
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
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<TClient, TMessageHandler>(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateDefaultLogger<TClient, TMessageHandler>();

            return HttpPolicyExtensions
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
        }

        private static IAsyncPolicy<HttpResponseMessage> GetNoRetryPolicy()
        {
            return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
        }
    }
}
