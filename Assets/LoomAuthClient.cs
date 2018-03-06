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

// TODO: Add auth flow for Android & OSX
// TODO: Add auth for Unity Web Player

public class LoomAccount
{
    public string Username { get; set; }
    public byte[] PrivateKey { get; set; }
}

public class LoomAuthClient
{
    private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

    private AuthenticationApiClient auth0Client;

    public LoomAuthClient()
    {
        this.auth0Client = new AuthenticationApiClient(new Uri("https://loomx.auth0.com"));
    }

    public async Task<LoomAccount> SignInFromNativeApp()
    {
        var accessToken = await this.GetAccessTokenForNativeApp();
        return await this.GetLoomAccount(accessToken);
        /*
        return new LoomAccount
        {
            Username = "",
            PrivateKey = new byte[] { }
        };
        */
    }

    // Implements Proof Key for Code Exchange (PKCE) auth flow for native desktop apps on Windows/Mac/Linux,
    // see also https://auth0.com/docs/api-auth/grant/authorization-code-pkce
    public async Task<string> GetAccessTokenForNativeApp()
    {
        // create a redirect URI using an available port on the loopback address.
        var redirectUrl = "http://127.0.0.1:9999/auth/auth0/";

        byte[] randomBytes = new byte[32];
        rngCsp.GetBytes(randomBytes);
        var codeVerifier = Convert.ToBase64String(randomBytes);
        string codeChallenge;
        using (var sha256 = SHA256.Create())
        {
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            codeChallenge = Base64UrlEncode(challengeBytes);
        }

        // create an HttpListener to listen for requests on that redirect URI.
        var http = new HttpListener();
        http.Prefixes.Add(redirectUrl);
        http.Start();
        // Debug.Log("Started listening");
        //statusTextRef.text = "Waiting for Auth0 Redirect...";
        Debug.Log("code_verifier: " + codeVerifier);

        var clientId = "25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud"; // unity3d sdk
        // var audience = "io.loomx.unity3d";
        var audience = "https://keystore.loomx.io/";

        // open external browser window with request to auth0 auth endpoint
        var authUrl = this.auth0Client.BuildAuthorizationUrl()
                .WithResponseType(AuthorizationResponseType.Code)
                .WithClient(clientId)
                .WithRedirectUrl(redirectUrl)
                .WithScope("openid profile email picture")
                .WithAudience(audience)
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
        var code = context.Request.QueryString["code"];
        //statusTextRef.text = "Auth Code Received";
        Debug.Log("code: " + code);

        // let the user know they can close the browser window
        var responseStr = "<HTML><BODY>You can close this window.</BODY></HTML>";
        byte[] responseBuffer = Encoding.UTF8.GetBytes(responseStr);
        context.Response.ContentLength64 = responseBuffer.Length;
        var outputStream = context.Response.OutputStream;
        outputStream.Write(responseBuffer, 0, responseBuffer.Length);
        outputStream.Close();

        http.Stop();
        Debug.Log("Stopped listening");

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
                ClientId = clientId,
                Code = code,
                CodeVerifier = codeVerifier,
                RedirectUri = redirectUrl
            });
            //statusTextRef.text = "Access Token Acquired!";
            Debug.Log("Access Token: " + response.AccessToken);
            Debug.Log("ID Token: " + response.IdToken);
            return response.AccessToken;
        }
        finally
        {
            ServicePointManager.ServerCertificateValidationCallback = oldValidationCallback;
        }
    }

    public async Task<LoomAccount> GetLoomAccount(string accessToken)
    {
        // TODO: this is closely modelled after the JS implementation in DelegateCall, so it's kinda awkward
        // exchange the Auth0 access token for a Vault client token
        var vaultClient = new VaultClient("https://stage-vault.delegatecall.com/v1/");
        var resp = await vaultClient.PutAsync<VaultCreateTokenResponse, VaultCreateTokenRequest>("auth/auth0/create_token", new VaultCreateTokenRequest
        {
            AccessToken = accessToken
        });
        vaultClient.Token = resp.Auth.ClientToken;
        var vaultStore = new VaultStore(vaultClient, "delegatecall/");
        var keys = await vaultStore.GetKeysAsync();
        if (keys.Length > 0)
        {
            // existing account
            var parts = keys[0].Split('/');
            var privateKey = await vaultStore.GetPrivateKeyAsync(keys[0]);
            return new LoomAccount
            {
                Username = parts[parts.Length - 1],
                PrivateKey = privateKey
            };
        }
        else
        {
            // new account
            Debug.Log("Creating new account");
            var profile = await this.auth0Client.GetUserInfoAsync(accessToken);
            var account = new LoomAccount
            {
                Username = profile.Email.Split('@')[0],
                PrivateKey = new byte[32] // TODO: generate a new private key
            };
            // TODO: connect to blockchain & create an account
            await vaultStore.SetAsync(account.Username, account.PrivateKey);
            return account;
        }
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
