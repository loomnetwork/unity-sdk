using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

    private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

    // Use this for initialization
    void Start () {
        
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void SignIn()
    {
        GetAuth0AccessToken();
    }

    async void GetAuth0AccessToken()
    {
        // TODO: start listening for http redirect on 127.0.0.1
        // create a redirect URI using an available port on the loopback address.
        var redirectUrl = "http://127.0.0.1:8080/auth/auth0/";

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
        statusTextRef.text = "Waiting for Auth0 Redirect...";
        Debug.Log("code_verifier: " + codeVerifier);

        var clientId = "25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud";

        // open external browser window with request to auth0 auth endpoint
        var client = new AuthenticationApiClient(new Uri("https://loomx.auth0.com"));
        var authUrl = client.BuildAuthorizationUrl()
                .WithResponseType(AuthorizationResponseType.Code)
                .WithClient(clientId)
                .WithRedirectUrl(redirectUrl)
                .WithScope("openid profile email picture")
                .WithAudience("io.loomx.unity3d")
                .WithValue("code_challenge", codeChallenge)
                .WithValue("code_challenge_method", "S256")
                .Build();
        Application.OpenURL(authUrl.ToString());

        // wait for the auth response & extract authorization code
        var context = await http.GetContextAsync();
        var code = context.Request.QueryString["code"];
        statusTextRef.text = "Auth Code Received";
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
            var response = await client.GetTokenAsync(new AuthorizationCodePkceTokenRequest
            {
                ClientId = clientId,
                Code = code,
                CodeVerifier = codeVerifier,
                RedirectUri = redirectUrl
            });
            statusTextRef.text = "Access Token Acquired!";
            Debug.Log("Access Token: " + response.AccessToken);
        }
        catch (Exception e)
        {
            statusTextRef.text = "Error: " + e.Message;
            throw e;
        }
        finally
        {
            ServicePointManager.ServerCertificateValidationCallback = oldValidationCallback;
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
