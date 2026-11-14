using System;
using System.Net;
using System.Text;

namespace LockerEtl.Tests.Api
{
    /// <summary>Returns a fixed response and remembers the request, so clients can be tested without a server.</summary>
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string body = "")
        {
            _statusCode = statusCode;
            _body = body;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }

        public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://localhost/api/v1/") };
    }
}