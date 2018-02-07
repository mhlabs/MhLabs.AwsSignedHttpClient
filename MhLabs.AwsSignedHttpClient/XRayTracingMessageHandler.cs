using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace MhLabs.AWSXRayHttpClientHandler2
{
    public class XRayTracingMessageHandler : HttpClientHandler
    {
        private readonly Func<HttpRequestMessage, string> _overrideSubSegmentNameFunc;

        public XRayTracingMessageHandler() : this(message => null)
        {
        }
        public XRayTracingMessageHandler(string overrideSubSegmentName) : this(message => overrideSubSegmentName)
        {
        }

        public XRayTracingMessageHandler(Func<HttpRequestMessage, string> overrideSubSegmentNameFunc)
        {
            _overrideSubSegmentNameFunc = overrideSubSegmentNameFunc;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ProcessRequest(request);
            HttpResponseMessage webResponse;
            try
            {
                var responseAsync = await base.SendAsync(request, cancellationToken);
                ProcessResponse(responseAsync);
                webResponse = responseAsync;
            }
            catch (Exception ex)
            {
                AWSXRayRecorder.Instance.AddException(ex);
                if (ex is HttpRequestException)
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    ProcessResponse(response);
                }
                throw;
            }
            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
            return webResponse;
        }

        private void ProcessResponse(HttpResponseMessage response)
        {
            if (AWSXRayRecorder.Instance.IsTracingDisabled())
                return;
            var dictionary = new Dictionary<string, object>();
            int statusCode = (int)response.StatusCode;
            dictionary["status"] = (object)statusCode;
            if (statusCode >= 400 && statusCode <= 499)
            {
                AWSXRayRecorder.Instance.MarkError();
                if (statusCode == 429)
                    AWSXRayRecorder.Instance.MarkThrottle();
            }
            else if (statusCode >= 500 && statusCode <= 599)
                AWSXRayRecorder.Instance.MarkFault();
            dictionary["content_length"] = (object)response.Content.Headers.ContentLength;
            AWSXRayRecorder.Instance.AddHttpInformation(nameof(response), (object)dictionary);
        }

        private void ProcessRequest(HttpRequestMessage request)
        {
            Console.WriteLine("IsTracingDisabled() = " + AWSXRayRecorder.Instance.IsTracingDisabled());
            Console.WriteLine("Subsegment name = " + _overrideSubSegmentNameFunc?.Invoke(request));
            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                AWSXRayRecorder.Instance.BeginSubsegment(_overrideSubSegmentNameFunc?.Invoke(request) != null ? _overrideSubSegmentNameFunc(request) : request.RequestUri.Host);
                AWSXRayRecorder.Instance.SetNamespace("remote");
                var dictionary =
                    new Dictionary<string, object>
                    {
                        ["url"] = request.RequestUri.AbsoluteUri,
                        ["method"] = request.Method.Method
                    };
                AWSXRayRecorder.Instance.AddHttpInformation(nameof(request), dictionary);
            }

            if (!TraceHeader.TryParse(TraceContext.GetEntity(), out var header))
                return;
            request.Headers.Add("X-Amzn-Trace-Id", header.ToString());
        }
    }
}
