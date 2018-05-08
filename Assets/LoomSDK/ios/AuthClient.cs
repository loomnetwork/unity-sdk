using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.InteropServices;

using UnityEngine;
using System.Collections;
using AOT;

namespace Loom.Unity3d.IOS
{
	#if UNITY_IOS && !UNITY_EDITOR
    internal class AuthConfig
    {
        public string ClientId;
        public string Domain;
        public string Audience;
        public string Scope;
    }

    internal class AuthClient : IAuthClient
    {

//
 
    [DllImport("__Internal")]
	private static extern void _ex_callGetAccessToken(string message,Action<string> OnSuccess,Action<string> OnError);
    
        //

        private AuthenticationApiClient auth0Client;
        public ILogger Logger { get; set; }

        public string ClientId { get; set; }
        public string Domain { get; set; }
        public string Audience { get; set; }
        public string Scope { get; set; }
		public static TaskCompletionSource<string> taskCompletionSource;
        public AuthClient()
        {
            this.Logger = NullLogger.Instance;
            this.auth0Client = new AuthenticationApiClient(new Uri("https://loomx.auth0.com"));
			taskCompletionSource = new TaskCompletionSource<string>();

        }
	   public delegate void CallbackDelegate(string str);

	[MonoPInvokeCallback(typeof(CallbackDelegate))]
		public static void OnSuccess(string accessToken)
		{
	taskCompletionSource.SetResult(accessToken);
		}
	[MonoPInvokeCallback(typeof(CallbackDelegate))]
	public static void OnFail(string str)
	{
	taskCompletionSource.SetException(new Exception(str));
	}
        public async Task<string> GetAccessTokenAsync()
        {
	var config= "{\"ClientId\" : \""+this.ClientId+"\",\"Domain\" : \""+this.Domain+"\",\"Audience\" : \""+this.Audience+"\",\"Scope\" : \""+this.Scope+"\"}";
 
	_ex_callGetAccessToken(config,OnSuccess,OnFail);
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
            UserInfo profile = await this.auth0Client.GetUserInfoAsync(accessToken);
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
	#endif
}
