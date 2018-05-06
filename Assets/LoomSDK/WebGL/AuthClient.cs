#if UNITY_WEBGL

using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace Loom.Unity3d.WebGL
{
    internal class AuthClient : IAuthClient
    {
        private class UserInfo
        {
            [JsonProperty("username")]
            public string Username { get; set; }
            [JsonProperty("key")]
            public string PrivateKey { get; set; }
        }

        // These functions are implemented and documented in Assets/LoomSDK/WebGL/LoomPlugin.jslib
        [DllImport("__Internal")]
        private static extern void StartLoomAuthFlow(string authHandlerName);
        [DllImport("__Internal")]
        private static extern string GetLoomUserInfo(string localStorageKey);

        private static readonly string LogTag = "Loom.WebGL.AuthClient";

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Local Storage key that should be used to lookup the user info.
        /// </summary>
        public string LocalStorageKey { get; set; }

        /// <summary>
        /// The name of the auth handler function that should be used by the WebGL AuthClient.
        /// It is expected to be attached to the `window` global.
        /// </summary>
        public string AuthHandlerName { get; set; }

        public AuthClient()
        {
            this.Logger = NullLogger.Instance;
        }

        /// <summary>
        /// In WebGL builds the access token should be obtained and handled in the host page,
        /// calling this method will throw a NotImplementedException.
        /// </summary>
        public Task<string> GetAccessTokenAsync()
        {
            throw new NotImplementedException("Access token retrieval must be handled by the host page.");
        }

        public async Task<Identity> GetIdentityAsync(string accessToken, IKeyStore keyStore)
        {
            var userInfo = JsonConvert.DeserializeObject<UserInfo>(GetLoomUserInfo(this.LocalStorageKey));
            if (string.IsNullOrEmpty(userInfo.Username) || string.IsNullOrEmpty(userInfo.PrivateKey))
            {
                StartLoomAuthFlow(this.AuthHandlerName);
                // poll local storage until the user info shows up
                while (true)
                {
                    await new WaitForSecondsRealtime(0.5f);
                    userInfo = JsonConvert.DeserializeObject<UserInfo>(GetLoomUserInfo(this.LocalStorageKey));
                    if (!string.IsNullOrEmpty(userInfo.Username) && !string.IsNullOrEmpty(userInfo.PrivateKey))
                    {
                        break;
                    }
                }
            }
            var privateKey = CryptoUtils.HexStringToBytes(userInfo.PrivateKey);
            return await Task.FromResult(new Identity
            {
                Username = userInfo.Username,
                PrivateKey = privateKey
            });
        }

        public async Task<Identity> CreateIdentityAsync(string accessToken, IKeyStore keyStore)
        {
            throw new NotImplementedException("Identity must be created by the host page.");
        }
    }
}

#endif