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

    public class BingService : IBingService
    {
        private readonly HttpClient _client;
        private readonly ILogger<BingService> _logger;

        public BingService(HttpClient httpClient, ILogger<BingService> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task Get(string query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Search Bing: {Query}", query);
            await _client.SendAsync<string>(HttpMethod.Get, $"search?q={query}", cancellationToken: cancellationToken);
        }
    }
}