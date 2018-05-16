using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections;
using AOT;
using UnityEngine;

namespace Loom.Unity3d.IOS
{
	#if UNITY_IOS && !UNITY_EDITOR

	//Wrapper class for Auth0.AuthenticationApi.Models.UserInfo json
	public class ProfileUserInfo
	{
		//
		// Properties
		//
		[JsonProperty ("email")]
		public string Email;

		//
		// Constructors
		//
		public ProfileUserInfo ()
		{

		}
	}

	internal class AuthConfig
	{
		public string ClientId;
		public string Domain;
		public string Audience;
		public string Scope;
	}

	internal class AuthClient : IAuthClient
	{

		[DllImport ("__Internal")]
		private static extern void _ex_callGetAccessToken (string message, Action<string> onSuccess, Action<string> onError);

		[DllImport ("__Internal")]
		private static extern void _ex_callGetUserProfile (string domain, string accessToken, Action<string> onResult);


		public ILogger Logger { get; set; }

		public string ClientId { get; set; }

		public string Domain { get; set; }

		public string Audience { get; set; }

		public string Scope { get; set; }

		public static TaskCompletionSource<string> taskCompletionSource;
		public static TaskCompletionSource<string> taskAuthSource;

		public AuthClient ()
		{
			this.Logger = NullLogger.Instance;
		}

		public delegate void CallbackDelegate (string returnStr);

		[MonoPInvokeCallback (typeof(CallbackDelegate))]
		public static void onSuccess (string accessToken)
		{
			taskCompletionSource.SetResult (accessToken);
		}

		[MonoPInvokeCallback (typeof(CallbackDelegate))]
		public static void onFail (string errorStr)
		{
			taskCompletionSource.SetException (new Exception (errorStr));
		}

		[MonoPInvokeCallback (typeof(CallbackDelegate))]
		public static void onAuthResult (string jsonString)
		{
			taskAuthSource.SetResult (jsonString);
		}

		public async Task<string> GetAccessTokenAsync ()
		{
			taskCompletionSource = new TaskCompletionSource<string> ();
			var config = JsonConvert.SerializeObject (new AuthConfig {
				ClientId = this.ClientId,
				Domain = this.Domain,
				Audience = this.Audience,
				Scope = this.Scope
			});
			_ex_callGetAccessToken (config, onSuccess, onFail);
			return await taskCompletionSource.Task;
		}

		public async Task<Identity> GetIdentityAsync (string accessToken, IKeyStore keyStore)
		{
			var keys = await keyStore.GetKeysAsync ();
			if (keys.Length > 0) {
				// existing account
				var parts = keys [0].Split ('/'); // TODO: This doesn't really do much atm
				var privateKey = await keyStore.GetPrivateKeyAsync (keys [0]);
				return new Identity {
					Username = parts [parts.Length - 1],
					PrivateKey = privateKey
				};
			} else {
				return await CreateIdentityAsync (accessToken, keyStore);
			}
		}

		/// <summary>
		/// Creates a new identity that can be used to sign transactions on the Loom DAppChain.
		/// </summary>
		/// <returns>A new <see cref="Identity"/>.</returns>
		public async Task<Identity> CreateIdentityAsync (string accessToken, IKeyStore keyStore)
		{
			taskAuthSource = new TaskCompletionSource<string> ();
			_ex_callGetUserProfile ("https://loomx.auth0.com"/*domain?*/, accessToken, onAuthResult);
			await taskAuthSource.Task;
			var profile = JsonConvert.DeserializeObject<ProfileUserInfo> (taskAuthSource.Task.Result);
			var identity = new Identity {
				Username = profile.Email.Split ('@') [0],
				PrivateKey = CryptoUtils.GeneratePrivateKey ()
			};

			// TODO: connect to blockchain & post a create an account Tx
			await keyStore.SetAsync (identity.Username, identity.PrivateKey);
			return identity;

		}

		public Task ClearIdentityAsync ()
		{
			// TODO
			throw new NotImplementedException ();
		}
	}
	#endif
}
