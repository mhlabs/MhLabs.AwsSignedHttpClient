using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
    }
}