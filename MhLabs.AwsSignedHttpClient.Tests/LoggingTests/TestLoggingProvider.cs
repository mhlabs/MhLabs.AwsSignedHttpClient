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
}