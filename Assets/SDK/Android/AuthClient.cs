using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Unity3d.Android
{
    internal class AuthClient : IAuthClient
    {
        private AuthenticationApiClient auth0Client;
        private string vaultPrefix;

        public string VaultPrefix
        {
            get
            {
                return this.vaultPrefix;
            }
            internal set
            {
                this.vaultPrefix = value.EndsWith("/") ? value : (value + "/");
            }
        }

        public AuthClient()
        {
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
                authFragment.CallStatic("login", loginCallback);
            }
            return await taskCompletionSource.Task;
        }

        public async Task<Identity> GetIdentityAsync(string accessToken)
        {
            // TODO: this is closely modelled after the JS implementation in DelegateCall, so it's kinda awkward
            // exchange the Auth0 access token for a Vault client token
            var vaultClient = new VaultClient("https://stage-vault.delegatecall.com/v1/");
            var resp = await vaultClient.PutAsync<VaultCreateTokenResponse, VaultCreateTokenRequest>("auth/auth0/create_token", new VaultCreateTokenRequest
            {
                AccessToken = accessToken
            });
            vaultClient.Token = resp.Auth.ClientToken;
            var vaultStore = new VaultStore(vaultClient, this.vaultPrefix);
            var keys = await vaultStore.GetKeysAsync();
            if (keys.Length > 0)
            {
                // existing account
                var parts = keys[0].Split('/'); // TODO: This doesn't really do much atm
                var privateKey = await vaultStore.GetPrivateKeyAsync(keys[0]);
                return new Identity
                {
                    Username = parts[parts.Length - 1],
                    PrivateKey = privateKey
                };
            }
            else
            {
                return await CreateIdentityAsync(accessToken, vaultStore);
            }
        }

        /// <summary>
        /// Creates a new identity that can be used to sign transactions on the Loom DAppChain.
        /// </summary>
        /// <returns>A new <see cref="Identity"/>.</returns>
        public async Task<Identity> CreateIdentityAsync(string accessToken, VaultStore vaultStore)
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
            // TODO: connect to blockchain & post a create an account Tx
            await vaultStore.SetAsync(identity.Username, identity.PrivateKey);
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
    }
}
