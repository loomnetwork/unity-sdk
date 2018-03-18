using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Loom.Unity3d
{
    // HACK: Get around Unity TLS exceptions that occur when sending a request to the
    //       Auth0 HTTPS token endpoint... or any other HTTPS endpoint - by bypassing cert validation...
    //       Unclear why the TLS exceptions occur, no root CAs in Mono? Mono unable to validate sha256 certs?
    //       Certs definitely need to be imported into Mono, but for now we'll leave that for the game developers.
    // https://answers.unity.com/questions/1381396/unity-ssl-tlsexception.html
    // https://answers.unity.com/questions/50013/httpwebrequestgetrequeststream-https-certificate-e.html
    // https://answers.unity.com/questions/792342/how-to-validate-ssl-certificates-when-using-httpwe.html
    // https://answers.unity.com/questions/1184815/how-to-stop-mono-from-preventing-authentication.html#answer-1186348
    // https://answers.unity.com/questions/1186445/what-is-the-best-way-to-add-root-certificates-to-a.html
    public class CertValidationBypass
    {
        public static RemoteCertificateValidationCallback oldCallback;

        private static bool ValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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

        public static void Enable()
        {
            oldCallback = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = ValidationCallback;
        }

        public static void Disable()
        {
            ServicePointManager.ServerCertificateValidationCallback = oldCallback;
        }
    }
}