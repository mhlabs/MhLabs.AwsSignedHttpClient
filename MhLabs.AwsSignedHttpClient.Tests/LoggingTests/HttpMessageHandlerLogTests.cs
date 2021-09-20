using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;
using Microsoft.Extensions.Http;
using MhLabs.AwsSignedHttpClient.Credentials;
using Moq;

namespace MhLabs.AwsSignedHttpClient.Tests
{

    public class HttpMessageHandlerLogTests
    {
        [Fact]
        public async Task Should_Register_Typed_Logger_Per_ClientAsync()
        {
            // Arrange
            var credentialsProvider = new Mock<ICredentialsProvider>();
            credentialsProvider.Setup(mock => mock.GetCredentials()).Returns(new AwsCredentials());

            var testProvider = new TestLoggingProvider();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(x => x.AddProvider(testProvider));
            serviceCollection.AddSingleton<ICredentialsProvider>(credentialsProvider.Object);

            serviceCollection.AddSignedHttpClient<IGoogleService, GoogleService>(new HttpOptions { BaseUrl = "https://www.google.com", RetryLevel = RetryLevel.Read });
            serviceCollection.AddUnsignedHttpClient<IBingService, BingService>(new HttpOptions { BaseUrl = "https://www.bing.com" });

            var provider = serviceCollection.BuildServiceProvider();
            var google = provider.GetService<IGoogleService>();
            var bing = provider.GetService<IBingService>();

            var cts = new CancellationTokenSource(5000);
            var token = cts.Token;

            // Act
            await google.Get("hello world", token);
            await bing.Get("hello world", token);

            // Assert
            TestLogger._logs.ShouldNotBeEmpty();

            // var filters = provider.GetServices<IHttpMessageHandlerBuilderFilter>();
            // foreach (var filter in filters)
            // {
            //     System.Console.WriteLine($"Filter: {filter} - {filter.GetType().Name}");
            // }

            // foreach (var log in TestConsoleLogger._logs)
            // {
            //     System.Console.WriteLine($"Name: {log.Name} - Log: {log.Log}");
            // }
        }

        [Fact]
        public async Task Should_Be_Able_To_Register_Custom_MessageHandler_Per_ClientAsync()
        {
            // Arrange
            var credentialsProvider = new Mock<ICredentialsProvider>();
            credentialsProvider.Setup(mock => mock.GetCredentials()).Returns(new AwsCredentials());

            var testProvider = new TestLoggingProvider();
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddLogging(x => x.AddProvider(testProvider));
            serviceCollection.AddSingleton<ICredentialsProvider>(credentialsProvider.Object);
            serviceCollection.AddTransient<TestDelegatingHandler>();

            serviceCollection.AddSignedHttpClientWitHttpClientBuilder<IGoogleService, GoogleService>(new HttpOptions { BaseUrl = "https://www.google.com", RetryLevel = RetryLevel.Read }).AddHttpMessageHandler<TestDelegatingHandler>();

            var provider = serviceCollection.BuildServiceProvider();
            var google = provider.GetService<IGoogleService>();

            var cts = new CancellationTokenSource(5000);
            var token = cts.Token;

            // Act
            await google.Get("hello world", token);

            // Assert
            TestLogger._logs.ShouldNotBeEmpty();

           
        }

        public class TestDelegatingHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
