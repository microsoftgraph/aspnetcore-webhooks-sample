using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Helpers
{
    public class AuthHandler : DelegatingHandler
    {
        private IAuthenticationProvider _authenticationProvider;

        public AuthHandler(IAuthenticationProvider authenticationProvider, HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
            _authenticationProvider = authenticationProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            await _authenticationProvider.AuthenticateRequestAsync(request);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
