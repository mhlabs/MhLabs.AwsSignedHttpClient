using System.Net;

namespace MhLabs.AwsSignedHttpClient
{
    public class ExecutionResult<T>
    {
        public bool Success => (int)StatusCode >= 200 && (int)StatusCode < 300;
        public HttpStatusCode StatusCode { get; }
        public T Data { get; }

        public ExecutionResult(HttpStatusCode statusCode, T data)
        {
            StatusCode = statusCode;
            Data = data;
        }
    }
}