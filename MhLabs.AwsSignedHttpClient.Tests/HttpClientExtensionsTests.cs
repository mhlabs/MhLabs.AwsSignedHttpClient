using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class HttpClientExtensionsTests
    {
        private class TestData
        {
            public string Something { get; set; }
        }

        [Fact]
        public void Should_Handle_Response_Content_When_It_Is_Error_Code()
        {
            const string data = "BAD_REQUEST";
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            var result = HttpClientExtensions.CreateExecutionResult<TestData>(response, data);

            Assert.Null(result.Data);
            Assert.Equal("BAD_REQUEST", result.Content);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.False(result.Success);
        }

        [Fact]
        public void Should_Handle_Null_Reponse_As_Data()
        {
            var result = HttpClientExtensions.ToData<TestData>(null);
            Assert.Null(result);
        }
        
        [Fact]
        public void Should_Timeout()
        {
            var _messageHandlerMock = new Mock<HttpMessageHandler>();
            _messageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => MockSendAsync(10));

            var httpClient = new HttpClient(_messageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://api.somewhere.com")
            };

            bool thrown = false;
            try
            {
                httpClient.SendAsyncTimeout<string>(HttpMethod.Get, "example/path", 1).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                thrown = true;
            }
            
            _messageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
            thrown.ShouldBe(true);
        }
        
        [Fact]
        public async Task Should_Timeout_With_Existing_Token()
        {
            var _messageHandlerMock = new Mock<HttpMessageHandler>();
            _messageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => MockSendAsync(10));

            var httpClient = new HttpClient(_messageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://api.somewhere.com")
            };

            using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

            bool thrown = false;
            try
            {
                await httpClient.SendAsyncTimeout<string>(HttpMethod.Get, "example/path", 100, cancellationToken: tokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                thrown = true;
            }
            
            _messageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
            thrown.ShouldBe(true);
            tokenSource.IsCancellationRequested.ShouldBe(true);
        }
        
        private static HttpResponseMessage MockSendAsync(int delayMs)
        {
            Thread.Sleep(delayMs);
            return new HttpResponseMessage();
        }
    }
}