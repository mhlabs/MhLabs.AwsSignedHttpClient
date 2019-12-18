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

        public static ILogger CreateDefaultLogger<TClient>(this ILoggerFactory loggerFactory, string type)
        {
            var name = $"{type}<{typeof(TClient).Name}>";
            return loggerFactory.CreateLogger(name) ?? FallbackFactory.CreateLogger(name);
        }

        public static ILogger CreateDefaultLogger<TClient, TMessageHandler>(this ILoggerFactory loggerFactory)
        {
            var name = $"{typeof(TMessageHandler).Name}<{typeof(TClient).Name}>";
            return loggerFactory.CreateLogger(name) ?? FallbackFactory.CreateLogger(name);
        }
    }
}

