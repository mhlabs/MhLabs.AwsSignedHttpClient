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

    public interface IGoogleService
    {
        Task Get(string query, CancellationToken cancellationToken = default(CancellationToken));
    }
}