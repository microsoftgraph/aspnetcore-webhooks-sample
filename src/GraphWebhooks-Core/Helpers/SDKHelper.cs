using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GraphWebhooks_Core.Helpers
{
    public class SDKHelper : ISDKHelper
    {
        private readonly ISampleAuthProvider authProvider;
        private GraphServiceClient graphClient = null;

        public SDKHelper(ISampleAuthProvider authProvider)
        {
            this.authProvider = authProvider;
        }

        // Get an authenticated Microsoft Graph Service client.
        public GraphServiceClient GetAuthenticatedClient(string tenantId)
        {
            graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                async (requestMessage) =>
                {

                    // This sample passes the tenant ID to the sample auth provider to use as a cache key.
                    string accessToken = await authProvider.GetUserAccessTokenAsync(tenantId);

                    // Append the access token to the request.
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // This header identifies the sample in the Microsoft Graph service. If extracting this code for your project please remove.
                    requestMessage.Headers.Add("SampleID", "aspnetcore-apponlytoken-webhooks-sample");
                }));
            return graphClient;
        }
    }

    public interface ISDKHelper
    {
        GraphServiceClient GetAuthenticatedClient(string userObjectId);
    }
}
