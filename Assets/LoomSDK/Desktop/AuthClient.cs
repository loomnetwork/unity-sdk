using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Unity3d.Desktop
{
    internal class AuthClient : IAuthClient
    {
        private AuthenticationApiClient auth0Client;

        public string ClientId { get; set; }
        public string Domain { get; set; }
        public string Scheme { get; set; }
        public string Audience { get; set; }
        public string Scope { get; set; }
        /// <summary>
        /// Url Auth0 should redirect to after a user signs in.
        /// </summary>
        public string RedirectUrl { get; set; }

        public AuthClient()
        {
            this.auth0Client = new AuthenticationApiClient(new Uri("https://loomx.auth0.com"));
        }

        /// <summary>
        /// Opens the default web browser to the Auth0 sign-in page, and retrieves an access token after
        /// the user signs in.
        /// </summary>
        /// 
        /// Implements Proof Key for Code Exchange (PKCE) auth flow for native desktop apps on
        /// Windows/Mac/Linux, see also https://auth0.com/docs/api-auth/grant/authorization-code-pkce
        /// <returns></returns>
        public async Task<string> GetAccessTokenAsync()
        {
            var codeVerifier = Convert.ToBase64String(LoomCrypto.GeneratePrivateKey());
            string codeChallenge;
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64UrlEncode(challengeBytes);
            }

            // create an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(this.RedirectUrl);
            http.Start();

            string authCode;

            try
            {
                // open external browser window with request to auth0 auth endpoint
                var authUrl = this.auth0Client.BuildAuthorizationUrl()
                        .WithResponseType(AuthorizationResponseType.Code)
                        .WithClient(this.ClientId)
                        .WithRedirectUrl(this.RedirectUrl)
                        .WithScope(this.Scope)
                        .WithAudience(this.Audience)
                        .WithValue("code_challenge", codeChallenge)
                        .WithValue("code_challenge_method", "S256")
                        .Build();

                Debug.Log(authUrl.AbsoluteUri);

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        Application.OpenURL(authUrl.AbsoluteUri);
                        break;
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        // NOTE: Application.OpenURL() doesn't seem to work on OSX
                        System.Diagnostics.Process.Start("open", authUrl.AbsoluteUri);
                        break;
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        System.Diagnostics.Process.Start("xdg-open", authUrl.AbsoluteUri);
                        break;
                    default:
                        throw new NotImplementedException("PKCE auth flow is not supported on the current platform");
                }

                // wait for the auth response & extract authorization code
                var context = await http.GetContextAsync();
                authCode = context.Request.QueryString["code"];

                // let the user know they can close the browser window
                var responseStr = "<HTML><BODY>You can close this window.</BODY></HTML>";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(responseStr);
                context.Response.ContentLength64 = responseBuffer.Length;
                var outputStream = context.Response.OutputStream;
                outputStream.Write(responseBuffer, 0, responseBuffer.Length);
                outputStream.Close();
            }
            finally
            {
                http.Stop();
                Debug.Log("Stopped listening");
            }

            // HACK: Get around Unity TLS exceptions that occur when sending a request to the
            //       Auth0 HTTPS token endpoint - bypassing cert validation...
            //       Unclear why the TLS exceptions occur, no root CAs in Mono? Mono unable to validate sha256 certs?
            //       fuck knows... fix it later!
            // https://answers.unity.com/questions/1381396/unity-ssl-tlsexception.html
            // https://answers.unity.com/questions/50013/httpwebrequestgetrequeststream-https-certificate-e.html
            // https://answers.unity.com/questions/792342/how-to-validate-ssl-certificates-when-using-httpwe.html
            // https://answers.unity.com/questions/1184815/how-to-stop-mono-from-preventing-authentication.html#answer-1186348
            // https://answers.unity.com/questions/1186445/what-is-the-best-way-to-add-root-certificates-to-a.html
            var oldValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;

            // exchange auth code for an access token
            try
            {
                var response = await this.auth0Client.GetTokenAsync(new AuthorizationCodePkceTokenRequest
                {
                    ClientId = this.ClientId,
                    Code = authCode,
                    CodeVerifier = codeVerifier,
                    RedirectUri = this.RedirectUrl
                });
                Debug.Log("Access Token: " + response.AccessToken);
                return response.AccessToken;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = oldValidationCallback;
            }
        }

        public async Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore)
        {
            var keys = await keyStore.GetKeysAsync();
            if (keys.Length > 0)
            {
                // existing account
                var parts = keys[0].Split('/'); // TODO: This doesn't really do much atm
                var privateKey = await keyStore.GetPrivateKeyAsync(keys[0]);
                return new Identity
                {
                    Username = parts[parts.Length - 1],
                    PrivateKey = privateKey
                };
            }
            else
            {
                return await CreateIdentityAsync(accessToken, keyStore);
            }
        }

        /// <summary>
        /// Creates a new identity that can be used to sign transactions on the Loom DAppChain.
        /// </summary>
        /// <returns>A new <see cref="Identity"/>.</returns>
        public async Task<Identity> CreateIdentityAsync(string accessToken, IKeyStore keyStore)
        {
            Debug.Log("Creating new account");
            var oldValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;
            UserInfo profile;
            try
            {
                profile = await this.auth0Client.GetUserInfoAsync(accessToken);
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = oldValidationCallback;
            }
            Debug.Log("Retrieved user profile");
            var identity = new Identity
            {
                Username = profile.Email.Split('@')[0],
                PrivateKey = LoomCrypto.GeneratePrivateKey()
            };
            await keyStore.SetAsync(identity.Username, identity.PrivateKey);
            return identity;
        }

        static bool CertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }

        // From https://github.com/IdentityModel/IdentityModel2 (src/IdentityModel/Base64Url.cs)
        static string Base64UrlEncode(byte[] buffer)
        {
            var s = Convert.ToBase64String(buffer); // Standard base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }
    }
}
