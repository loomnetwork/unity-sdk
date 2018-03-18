using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Unity3d.Desktop
{
    internal class AuthClient : IAuthClient
    {
        private static readonly string LogTag = "Loom.Desktop.AuthClient";

        private AuthenticationApiClient auth0Client;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

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
            this.Logger = NullLogger.Instance;
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
            var codeVerifier = Convert.ToBase64String(CryptoUtils.GeneratePrivateKey());
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
                Logger.Log(LogTag, "Stopped listening");
            }

            // exchange auth code for an access token
            var response = await this.auth0Client.GetTokenAsync(new AuthorizationCodePkceTokenRequest
            {
                ClientId = this.ClientId,
                Code = authCode,
                CodeVerifier = codeVerifier,
                RedirectUri = this.RedirectUrl
            });
            Logger.Log(LogTag, "Access Token: " + response.AccessToken);
            return response.AccessToken;
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
            Logger.Log(LogTag, "Creating new account");
            UserInfo profile = await this.auth0Client.GetUserInfoAsync(accessToken);
            Logger.Log(LogTag, "Retrieved user profile");
            var identity = new Identity
            {
                Username = profile.Email.Split('@')[0],
                PrivateKey = CryptoUtils.GeneratePrivateKey()
            };
            await keyStore.SetAsync(identity.Username, identity.PrivateKey);
            return identity;
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
