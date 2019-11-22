using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;

namespace MhLabs.AwsSignedHttpClient.Tests
{

    public class HttpMessageHandlerLogTests
    {
        [Fact]
        public async Task Should_Register_Typed_Logger_Per_ClientAsync()
        {
            // Arrange
            var testProvider = new TestLoggingProvider();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(x => x.AddProvider(testProvider));
            serviceCollection.AddSignedHttpClient<IGoogleService, GoogleService>();
            serviceCollection.AddSignedHttpClient<IBingService, BingService>();

            var provider = serviceCollection.BuildServiceProvider();
            var google = provider.GetService<IGoogleService>();
            var bing = provider.GetService<IBingService>();

            var cts = new CancellationTokenSource(5000);
            var token = cts.Token;

            // Act
            await google.Get("hello world", token);
            await bing.Get("hello world", token);

            // Assert
            TestConsoleLogger._logs.ShouldNotBeEmpty();
        }
    }

    public class TestLoggingProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new TestConsoleLogger(categoryName);
        }

        public void Dispose()
        {
            
        }
    }

    public interface IGoogleService
    {
        Task Get(string query, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class GoogleService : IGoogleService
    {
        private readonly HttpClient _client;
        private readonly ILogger<GoogleService> _logger;

        public GoogleService(HttpClient httpClient, ILogger<GoogleService> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task Get(string query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Search Google: {Query}", query);

            var response = await _client.SendAsync<object>(HttpMethod.Get, "test", cancellationToken: cancellationToken);
            _logger.LogInformation("Orders response: {Response}", response);
        }
    }

    public interface IBingService
    {
        Task Get(string query, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class BingService : IBingService
    {
        private readonly HttpClient _client;
        private readonly ILogger<BingService> _logger;

        public BingService(HttpClient httpClient, ILogger<BingService> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task Get(string query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Search Bing: {Query}", query);

            var response = await _client.SendAsync<object>(HttpMethod.Get, "test", cancellationToken: cancellationToken);

            _logger.LogInformation("Orders response: {Response}", response);
        }
    }

    public class TestConsoleLogger : ILogger
    {
        static internal readonly List<TestLog> _logs = new List<TestLog>();
        private string categoryName;

        public TestConsoleLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            
            var log = $"{logLevel.ToString()} - {categoryName} - {eventId.Id} - {formatter(state, exception)}";
            Console.WriteLine(log);
            _logs.Add(new TestLog {
                Name = categoryName,
                Log = log
            });
        }
    }

    public class TestLog
    {
        public string Name { get; set; }
        public string Log { get; set; }
    }
}