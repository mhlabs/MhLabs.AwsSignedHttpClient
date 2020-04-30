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

    public class TestLogger : ILogger
    {
        static internal readonly List<TestLog> _logs = new List<TestLog>();
        private string categoryName;

        public TestLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var log = $"{logLevel.ToString()} - {categoryName} - {eventId.Id} - {formatter(state, exception)}";
            _logs.Add(new TestLog
            {
                Name = categoryName,
                Log = log
            });
        }
    }
}