using UnityEngine;

namespace Loom.Unity3d
{
    public class AuthClientFactory
    {
        private ILogger logger;
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

        public AuthClientFactory WithLogger(ILogger logger)
        {
            this.logger = logger;
            return this;
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

#if UNITY_WEBGL
        private WebGL.HostPageHandlers hostPageHandlers;

        /// <summary>
        /// Sets the names of the auth related functions in the host page that should be used by the WebGL AuthClient.
        /// </summary>
        public AuthClientFactory WithHostPageHandlers(WebGL.HostPageHandlers handlers)
        {
            this.hostPageHandlers = handlers;
            return this;
        }
#endif
        public IAuthClient Create()
        {

#if UNITY_ANDROID
            return new Android.AuthClient
            {
                Logger = this.logger ?? NullLogger.Instance,
                ClientId = this.clientId,
                Domain = this.domain,
                Scheme = this.scheme,
                Audience = this.audience,
                Scope = this.scope
            };
#elif UNITY_IOS&&!UNITY_EDITOR
            return new IOS.AuthClient
            {
                Logger = this.logger ?? NullLogger.Instance,
                ClientId = this.clientId,
                Domain = this.domain,
                Audience = this.audience,
                Scope = this.scope
            };
#elif UNITY_EDITOR || UNITY_STANDALONE
            return new Desktop.AuthClient
            {
                Logger = this.logger ?? NullLogger.Instance,
                ClientId = this.clientId,
                Domain = this.domain,
                Scheme = this.scheme,
                Audience = this.audience,
                Scope = this.scope,
                RedirectUrl = this.redirectUrl
            };
#elif UNITY_WEBGL
            return new WebGL.AuthClient
            {
                Logger = this.logger ?? NullLogger.Instance,
                HostPageHandlers = this.hostPageHandlers
            };
#else
            throw new System.NotImplementedException();
#endif
        }
    }
}
