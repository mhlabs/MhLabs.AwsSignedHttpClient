using System;
using Xunit;

namespace MhLabs.AwsSignedHttpClient.Tests
{
    public class SignV4UtilTests
    {
        [Fact]
        public void GetCanonicalQueryStringTest()
        {
            var str = new Uri("https://api.mhdev.seproduct-search/int/stores/10/products?productId=123,456,789").GetCanonicalQueryString();

        }
    }
}
