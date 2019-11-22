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
            serviceCollection.AddSignedHttpClientWithFileCredentials<IGoogleService, GoogleService>(new HttpOptions { BaseUrl = "https://www.google.com"} );
            serviceCollection.AddSignedHttpClientWithFileCredentials<IBingService, BingService>(new HttpOptions { BaseUrl = "https://www.bing.com"} );
            
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
}