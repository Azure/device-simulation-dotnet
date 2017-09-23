// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        HttpResponseHeaders Headers { get; }
        string Content { get; }

        bool IsSuccess { get; }
        bool IsError { get; }
        bool IsIncomplete { get; }
        bool IsNonRetriableError { get; }
        bool IsRetriableError { get; }
        bool IsBadRequest { get; }
        bool IsUnauthorized { get; }
        bool IsForbidden { get; }
        bool IsNotFound { get; }
        bool IsTimeout { get; }
        bool IsConflict { get; }
        bool IsServerError { get; }
        bool IsServiceUnavailable { get; }
    }

    public class HttpResponse : IHttpResponse
    {
        private const int TOO_MANY_REQUESTS = 429;

        public HttpResponse()
        {
        }

        public HttpResponse(
            HttpStatusCode statusCode,
            string content,
            HttpResponseHeaders headers)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
            this.Content = content;
        }

        public HttpStatusCode StatusCode { get; internal set; }
        public HttpResponseHeaders Headers { get; internal set; }
        public string Content { get; internal set; }

        public bool IsSuccess => (int) this.StatusCode >= 200 && (int) this.StatusCode <= 299;
        public bool IsError => (int) this.StatusCode >= 400 || (int) this.StatusCode == 0;

        public bool IsIncomplete
        {
            get
            {
                var c = (int) this.StatusCode;
                return (c >= 100 && c <= 199) || (c >= 300 && c <= 399);
            }
        }

        public bool IsNonRetriableError => this.IsError && !this.IsRetriableError;

        public bool IsRetriableError => this.StatusCode == HttpStatusCode.NotFound ||
                                        this.StatusCode == HttpStatusCode.RequestTimeout ||
                                        (int) this.StatusCode == TOO_MANY_REQUESTS;

        public bool IsBadRequest => (int) this.StatusCode == 400;
        public bool IsUnauthorized => (int) this.StatusCode == 401;
        public bool IsForbidden => (int) this.StatusCode == 403;
        public bool IsNotFound => (int) this.StatusCode == 404;
        public bool IsTimeout => (int) this.StatusCode == 408;
        public bool IsConflict => (int) this.StatusCode == 409;
        public bool IsServerError => (int) this.StatusCode >= 500;
        public bool IsServiceUnavailable => (int) this.StatusCode == 503;
    }
}
