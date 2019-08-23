using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class BaseHttpMessageHandlerTests
    {
        [Fact]
        public void Should_Get_Inherited_Class_Name()
        {
            var child = new AwsSignedHttpMessageHandler();

            Assert.Equal("AwsSignedHttpMessageHandler", child.ImplementingName);
        }
    }
}