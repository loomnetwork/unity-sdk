using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Loom.Unity3d
{
    public class VaultError : System.Exception
    {
        public string[] Errors;

        public VaultError(long httpStatusCode, string[] errors = null) : base(FormatMessage(httpStatusCode, errors))
        {
            this.Errors = errors;
        }

        private static string FormatMessage(long httpStatusCode, string[] errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("HTTP Status {0}:", httpStatusCode));
            if (errors != null)
            {
                for (int i = 0; i < errors.Length; i++)
                {
                    sb.AppendLine(errors[i]);
                }
            }
            return sb.ToString();
        }
    }

    public class VaultResponse
    {
        [JsonProperty("request_id")]
        string RequestId { get; set; }
    }

    public class VaultCreateTokenRequest
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public class VaultCreateTokenResponse
    {
        public class AuthData
        {
            [JsonProperty("client_token")]
            public string ClientToken { get; set; }
        }

        [JsonProperty("auth")]
        public AuthData Auth { get; set; }
    }

    public class VaultStorePrivateKeyRequest
    {
        [JsonProperty("privateKey")]
        public string PrivateKey { get; set; }
    }

    public class VaultGetPrivateKeyResponse : VaultResponse
    {
        public class KeyData
        {
            [JsonProperty("privateKey")]
            public string PrivateKey { get; set; }
        }

        [JsonProperty("data")]
        public KeyData Data;
    }

    public class VaultListSecretsResponse : VaultResponse
    {
        public class KeyData
        {
            [JsonProperty("keys")]
            public string[] Keys { get; set; }
        }

        [JsonProperty("data")]
        public KeyData Data { get; set; }
    }

    public class VaultErrorResponse
    {
        [JsonProperty("errors")]
        public string[] Errors { get; set; }
    }

    public class VaultClient
    {
        private static readonly string LogTag = "Loom.VaultClient";

        private string url;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public string Token { get; set; }

        public VaultClient(string url, string token = null)
        {
            this.url = url;
            this.Logger = NullLogger.Instance;
            this.Token = token;
        }

        public async Task<VaultListSecretsResponse> ListAsync(string path)
        {
            using (var r = new UnityWebRequest(this.url + path + "?list=true", "GET"))
            {
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                SetRequestHeaders(r);
                await r.SendWebRequest();
                HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "HTTP response body: " + r.downloadHandler.text);
                    return JsonConvert.DeserializeObject<VaultListSecretsResponse>(r.downloadHandler.text);
                }
                return null;
            }
        }

        public async Task<T> GetAsync<T>(string path)
        {
            using (var r = UnityWebRequest.Get(this.url + path))
            {
                SetRequestHeaders(r);
                await r.SendWebRequest();
                HandleError(r);
                Logger.Log(LogTag, "HTTP response body: " + r.downloadHandler.text);
                return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
            }
        }

        public async Task<T> PutAsync<T, U>(string path, U data)
        {
            string body = JsonConvert.SerializeObject(data);
            Logger.Log("PutAsync JSON body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.url + path, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                SetRequestHeaders(r);
                await r.SendWebRequest();
                HandleError(r);
                Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
                return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
            }
        }

        public async Task PutAsync<T>(string path, T data)
        {
            string body = JsonConvert.SerializeObject(data);
            Logger.Log(LogTag, "PutAsync JSON body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.url + path, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                SetRequestHeaders(r);
                await r.SendWebRequest();
                HandleError(r);
                Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
            }
        }

        private void SetRequestHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            if (!String.IsNullOrEmpty(this.Token))
            {
                request.SetRequestHeader("X-Vault-Token", this.Token);
            }
        }

        private void HandleError(UnityWebRequest r)
        {
            if (r.isNetworkError)
            {
                throw new System.Exception(String.Format("HTTP '{0}' request to '{1}' failed", r.method, r.url));
            }
            else if (r.isHttpError)
            {
                string[] errors = null;
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    var data = JsonConvert.DeserializeObject<VaultErrorResponse>(r.downloadHandler.text);
                    errors = data.Errors;
                }
                throw new VaultError(r.responseCode, errors);
            }
        }
    }
}