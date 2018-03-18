using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Unity3d.Android
{
    internal class AuthConfig
    {
        public string ClientId;
        public string Domain;
        public string Scheme;
        public string Audience;
        public string Scope;
    }

    internal class AuthClient : IAuthClient
    {
        private static readonly string LogTag = "Loom.Android.AuthClient";

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

        public AuthClient()
        {
            this.Logger = NullLogger.Instance;
            this.auth0Client = new AuthenticationApiClient(new Uri("https://loomx.auth0.com"));
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<string>();
            var loginCallback = new LoginCallback()
            {
                OnSuccess = (string accessToken) => taskCompletionSource.SetResult(accessToken),
                OnFailure = (string exception) => taskCompletionSource.SetException(new Exception(exception))
            };
            using (var authFragment = new AndroidJavaClass("io.loomx.unity3d.AuthFragment"))
            {
                authFragment.CallStatic("start"); // attach to current Unity activity
                var config = JsonConvert.SerializeObject(new AuthConfig
                {
                    ClientId = this.ClientId,
                    Domain = this.Domain,
                    Scheme = this.Scheme,
                    Audience = this.Audience,
                    Scope = this.Scope
                });
                authFragment.CallStatic("login", config, loginCallback);
            }
            return await taskCompletionSource.Task;
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
            // TODO: connect to blockchain & post a create an account Tx
            await keyStore.SetAsync(identity.Username, identity.PrivateKey);
            return identity;
        }
    }
}
