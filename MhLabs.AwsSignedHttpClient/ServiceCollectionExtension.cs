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
            services.AddTransient<AwsSignedHttpMessageHandler>();

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
            if (options == null) options = new HttpOptions(NullLogger.Instance);

            var logger = options.Logger; 
            var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(client =>
                {
                    client.BaseAddress = GetBaseUrl(options);
                }).AddHttpMessageHandler<AwsSignedHttpMessageHandler>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (options.RetryLevel == RetryLevel.Update)
            {
                httpClientBuilder
                    .AddPolicyHandler(GetRetryPolicy(logger));
            }

            if (options.RetryLevel == RetryLevel.Read)
            {
                httpClientBuilder
                    .AddPolicyHandler(request =>
                        request.Method == HttpMethod.Get
                            ? GetRetryPolicy(logger)
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

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrInner<IOException>()
                .WaitAndRetryAsync(3,
                            retryAttempt =>
                            {
                                var delay = TimeSpan.FromMilliseconds(Math.Pow(5, retryAttempt))
                                    + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                                
                                logger.LogInformation("AwsSignedHttpClient - Retrying call, attempt: {RetryAttempt}, delay ms: {Delay}", retryAttempt, delay.TotalMilliseconds);
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