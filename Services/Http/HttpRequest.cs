// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http
{
    public interface IHttpRequest
    {
        Uri Uri { get; set; }

        HttpHeaders Headers { get; }

        MediaTypeHeaderValue ContentType { get; }

        HttpRequestOptions Options { get; }

        HttpContent Content { get; }

        IHttpRequest AddHeader(string name, string value);

        IHttpRequest SetUriFromString(string uri);

        IHttpRequest SetContent(string content);

        IHttpRequest SetContent(string content, Encoding encoding);

        IHttpRequest SetContent(string content, Encoding encoding, string mediaType);

        IHttpRequest SetContent(string content, Encoding encoding, MediaTypeHeaderValue mediaType);

        IHttpRequest SetContent(StringContent stringContent);

        IHttpRequest SetContent<T>(T sourceObject);

        IHttpRequest SetContent<T>(T sourceObject, Encoding encoding);

        IHttpRequest SetContent<T>(T sourceObject, Encoding encoding, string mediaType);

        IHttpRequest SetContent<T>(T sourceObject, Encoding encoding, MediaTypeHeaderValue mediaType);
    }

    public class HttpRequest : IHttpRequest
    {
        private readonly MediaTypeHeaderValue defaultMediaType = new MediaTypeHeaderValue("application/json");
        private readonly Encoding defaultEncoding = new UTF8Encoding();

        // Http***Headers classes don't have a public ctor, so we use this class
        // to hold the headers, this is also used for PUT/POST requests body
        private readonly HttpRequestMessage requestContent = new HttpRequestMessage();

        public Uri Uri { get; set; }

        public HttpHeaders Headers => this.requestContent.Headers;

        public MediaTypeHeaderValue ContentType { get; private set; }

        public HttpRequestOptions Options { get; } = new HttpRequestOptions();

        public HttpContent Content => this.requestContent.Content;

        public HttpRequest()
        {
        }

        public HttpRequest(Uri uri)
        {
            this.Uri = uri;
        }

        public HttpRequest(string uri)
        {
            this.SetUriFromString(uri);
        }

        public IHttpRequest AddHeader(string name, string value)
        {
            if (!this.Headers.TryAddWithoutValidation(name, value))
            {
                if (name.ToLowerInvariant() != "content-type")
                {
                    throw new ArgumentOutOfRangeException(name, "Invalid header name");
                }

                this.ContentType = new MediaTypeHeaderValue(value);
            }

            return this;
        }

        public IHttpRequest SetUriFromString(string uri)
        {
            this.Uri = new Uri(uri);
            return this;
        }

        public IHttpRequest SetContent(string content)
        {
            return this.SetContent(content, this.defaultEncoding, this.defaultMediaType);
        }

        public IHttpRequest SetContent(string content, Encoding encoding)
        {
            return this.SetContent(content, encoding, this.defaultMediaType);
        }

        public IHttpRequest SetContent(string content, Encoding encoding, string mediaType)
        {
            return this.SetContent(content, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public IHttpRequest SetContent(string content, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            this.requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            this.ContentType = mediaType;
            return this;
        }

        public IHttpRequest SetContent(StringContent stringContent)
        {
            this.requestContent.Content = stringContent;
            this.ContentType = stringContent.Headers.ContentType;
            return this;
        }

        public IHttpRequest SetContent<T>(T sourceObject)
        {
            return this.SetContent(sourceObject, this.defaultEncoding, this.defaultMediaType);
        }

        public IHttpRequest SetContent<T>(T sourceObject, Encoding encoding)
        {
            return this.SetContent(sourceObject, encoding, this.defaultMediaType);
        }

        public IHttpRequest SetContent<T>(T sourceObject, Encoding encoding, string mediaType)
        {
            return this.SetContent(sourceObject, encoding, new MediaTypeHeaderValue(mediaType));
        }

        public IHttpRequest SetContent<T>(T sourceObject, Encoding encoding, MediaTypeHeaderValue mediaType)
        {
            var content = JsonConvert.SerializeObject(sourceObject, Formatting.None);
            this.requestContent.Content = new StringContent(content, encoding, mediaType.MediaType);
            this.ContentType = mediaType;
            return this;
        }
    }
}
