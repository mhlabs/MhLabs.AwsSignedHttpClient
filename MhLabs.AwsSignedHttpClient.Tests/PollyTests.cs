using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class PollyTests
    {
        public class HttpClientFactory_Polly_Policy_Test
        {
            [Fact]
            public async Task Playground_for_verifying_polly_behaviour()
            {
                IServiceCollection services = new ServiceCollection();

                var clientId = "x";
                var retryCount = 0;
                var exceptionCount = 0;

                try
                {
                    var policy =
                        HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .OrInner<IOException>()
                            .RetryAsync(3, onRetry: (_, __) =>
                            {
                                retryCount++;
                                Console.WriteLine("I have tried again!");
                            });

                    services.AddHttpClient(clientId)
                        .AddPolicyHandler(policy)
                        .AddHttpMessageHandler(() => new StubDelegatingHandler());

                    HttpClient configuredClient =
                        services
                            .BuildServiceProvider()
                            .GetRequiredService<IHttpClientFactory>()
                            .CreateClient(clientId);

                    // When / Act
                    var result = await configuredClient.GetAsync("https://www.non-existing-address-not-used-anyway.com/");
                }
                catch
                {
                    // will actually generate 4 exceptions but only last one should be propagated to calling code
                    exceptionCount++;
                }

                Assert.Equal(3, retryCount);
                Assert.Equal(1, exceptionCount);
            }
        }

        public class StubDelegatingHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var exceptions = new List<Exception>
                {
                    new HttpRequestException("An error occurred while sending the request."),
                    new IOException("Connection reset by PEER?!"),
                    new SocketException()
                };

                throw new AggregateException(exceptions);
            }
        }
    }
}