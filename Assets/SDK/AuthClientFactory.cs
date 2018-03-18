namespace Loom.Unity3d
{
    public class AuthClientFactory
    {
        private string clientId;
        private string domain;
        private string scheme;
        private string audience;
        private string scope;
        private string redirectUrl;

        public static AuthClientFactory Configure()
        {
            return new AuthClientFactory();
        }

        public AuthClientFactory WithClientId(string clientId)
        {
            this.clientId = clientId;
            return this;
        }

        public AuthClientFactory WithDomain(string domain)
        {
            this.domain = domain;
            return this;
        }

        public AuthClientFactory WithScheme(string scheme)
        {
            this.scheme = scheme;
            return this;
        }

        public AuthClientFactory WithAudience(string audience)
        {
            this.audience = audience;
            return this;
        }

        public AuthClientFactory WithScope(string scope)
        {
            this.scope = scope;
            return this;
        }

        public AuthClientFactory WithRedirectUrl(string url)
        {
            this.redirectUrl = url;
            return this;
        }

        public IAuthClient Create()
        {
#if UNITY_ANDROID
            return new Android.AuthClient
            {
                ClientId = this.clientId,
                Domain = this.domain,
                Scheme = this.scheme,
                Audience = this.audience,
                Scope = this.scope
            };
#else
            return new Desktop.AuthClient
            {
                ClientId = this.clientId,
                Domain = this.domain,
                Scheme = this.scheme,
                Audience = this.audience,
                Scope = this.scope,
                RedirectUrl = this.redirectUrl
            };
#endif
        }
    }
}
