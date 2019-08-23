using System.Net;
using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class ExecutionResultTests
    {
        [Fact]
        public void Should_Determine_Success()
        {
            var result = new ExecutionResult<string>(HttpStatusCode.NoContent, null);
            Assert.True(result.Success);
        }

        [Fact]
        public void Should_Determine_Failutre()
        {
            var result = new ExecutionResult<string>(HttpStatusCode.Unauthorized, null);
            Assert.False(result.Success);
        }

    }
}