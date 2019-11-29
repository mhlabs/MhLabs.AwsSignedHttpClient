using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ILoggerFactoryExtensions
    {
        private static ILoggerFactory FallbackFactory = InitiFallback();
        
        private static ILoggerFactory InitiFallback()
        {
            if (FallbackFactory == null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddLogging(builder => builder.AddConsole());

                using (var serviceProvider = serviceCollection.BuildServiceProvider())
                using (var loggerFactory = serviceProvider.GetService<ILoggerFactory>())
                {
                    return loggerFactory;
                }
            }
            return FallbackFactory;
        }

        public static ILogger CreateDefaultLogger<TClient>(this ILoggerFactory loggerFactory)
        {
            const string name = "AwsSignedHttpMessageHandler<{typeof(TClient).Name}>";

            if (loggerFactory == null) return NullLogger.Instance;

            return loggerFactory.CreateLogger(name) ?? FallbackFactory.CreateLogger(name);
        }
    }
}

