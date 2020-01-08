using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MhLabs.AwsSignedHttpClient
{
    public static class ILoggerFactoryExtensions
    {
        private static readonly ILoggerFactory FallbackFactory = InitiFallback();

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

        public static ILogger CreateDefaultLogger(this ILoggerFactory loggerFactory, string type, string client = null)
        {
            var name = client == null ? type : $"{type}<{client}>";
            return loggerFactory.CreateLogger(name) ?? FallbackFactory.CreateLogger(name);
        }

        public static ILogger CreateDefaultLogger<TClient, TMessageHandler>(this ILoggerFactory loggerFactory)
        {
            return CreateDefaultLogger(loggerFactory, typeof(TMessageHandler).Name, typeof(TClient).Name);
        }
    }
}

