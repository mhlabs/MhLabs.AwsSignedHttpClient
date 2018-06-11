using System.Net.Http;
using System.Threading.Tasks;

namespace MhLabs.AwsSignedHttpClient
{
    public interface IAwsHttpClient
    {
         Task<TReturn> SendAsync<TReturn>(HttpMethod method, string path, object postData = null,
            string contentType = "application/json") where TReturn : class;
    }
}