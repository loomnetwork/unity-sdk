#if UNITY_WEBGL

using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace Loom.Unity3d.WebGL
{
    public class HostPageHandlers
    {
        /// <summary>
        /// Name of handler that starts the auth-flow.
        /// </summary>
        public string SignIn;
        /// <summary>
        /// Name of handler that looks up user info stored in the host page.
        /// </summary>
        public string GetUserInfo;
        /// <summary>
        /// Name of handler that clears out any user info stored in the host page.
        /// </summary>
        public string SignOut;
    }

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
        private static extern void StartLoomAuthFlow(string handlerName);
        [DllImport("__Internal")]
        private static extern string GetLoomUserInfo(string handlerName);
        [DllImport("__Internal")]
        private static extern void ClearLoomUserInfo(string handlerName);

        private static readonly string LogTag = "Loom.WebGL.AuthClient";

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }
                
        /// <summary>
        /// Mapping of auth related function names that are implemented in the host page.
        /// These functions are expected to be attached to the `window` global in the host page.
        /// </summary>
        public HostPageHandlers HostPageHandlers;

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
            if (this.HostPageHandlers == null || string.IsNullOrEmpty(this.HostPageHandlers.GetUserInfo))
            {
                throw new Exception("GetUserInfo handler not set.");
            }
            var userInfo = JsonConvert.DeserializeObject<UserInfo>(GetLoomUserInfo(this.HostPageHandlers.GetUserInfo));
            if (string.IsNullOrEmpty(userInfo.Username) || string.IsNullOrEmpty(userInfo.PrivateKey))
            {
                if (this.HostPageHandlers == null || string.IsNullOrEmpty(this.HostPageHandlers.SignIn))
                {
                    throw new Exception("SignIn handler not set.");
                }
                StartLoomAuthFlow(this.HostPageHandlers.SignIn);
                var startTime = Time.time;
                var isTimedOut = false;
                // poll local storage until the user info shows up
                while (!isTimedOut)
                {
                    await new WaitForSecondsRealtime(0.5f);
                    userInfo = JsonConvert.DeserializeObject<UserInfo>(GetLoomUserInfo(this.HostPageHandlers.GetUserInfo));
                    if (!string.IsNullOrEmpty(userInfo.Username) && !string.IsNullOrEmpty(userInfo.PrivateKey))
                    {
                        break;
                    }
                    // keep trying for about 60 secs (though probably should make this configurable)
                    isTimedOut = (Time.time - startTime) > 60.0f;
                }
                if (isTimedOut)
                {
                    throw new Exception("User is not signed in.");
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

        public Task ClearIdentityAsync()
        {
            if (this.HostPageHandlers == null || string.IsNullOrEmpty(this.HostPageHandlers.SignOut))
            {
                throw new Exception("SignOut handler not set.");
            }
            ClearLoomUserInfo(this.HostPageHandlers.SignOut);
            return Task.CompletedTask;
        }
    }
}

#endif