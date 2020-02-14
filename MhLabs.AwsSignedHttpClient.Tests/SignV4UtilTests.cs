using System;
using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class SignV4UtilTests
    {
        [Fact]
        public void Should_Allow_List_Values_In_Query_String()
        {
            var str = new Uri("http://some.where/x?y=1&y=2").GetCanonicalQueryString();
            Assert.False(string.IsNullOrWhiteSpace(str));
        }
    }
}
