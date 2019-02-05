using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MhLabs.AwsSignedHttpClient
{
    public interface IAwsHttpClient
    {
         Task<TReturn> SendAsync<TReturn>(HttpMethod method, string path, object postData = null,
            string contentType = "application/json", CancellationToken cancellationToken = default(CancellationToken)) where TReturn : class;
    }
}