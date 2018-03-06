using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class VaultError : System.Exception
{
    public string[] Errors;

    public VaultError(long httpStatusCode, string[] errors = null): base(FormatMessage(httpStatusCode, errors))
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

public class VaultCreateTokenRequest
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }
}

public class VaultAuthData
{
    [JsonProperty("client_token")]
    public string ClientToken { get; set; }
}

public class VaultCreateTokenResponse
{
    [JsonProperty("auth")]
    public VaultAuthData Auth { get; set; }
}

public class VaultStorePrivateKeyRequest
{
    [JsonProperty("privateKey")]
    public string PrivateKey { get; set; }
}

public class VaultGetPrivateKeyResponse
{
    [JsonProperty("privateKey")]
    public string PrivateKey { get; set; }
}

public class VaultKeyList
{
    [JsonProperty("keys")]
    public string[] Keys { get; set; }
}

public class VaultErrorResponse
{
    [JsonProperty("errors")]
    public string[] Errors { get; set; }
}

public class VaultClient {
    
    private string url;
    public string Token { get; set; }

    public VaultClient(string url, string token = null)
    {
        this.url = url;
        this.Token = token;
    }

    public async Task<VaultKeyList> ListAsync(string path)
    {
        using (var r = new UnityWebRequest(this.url + path, "LIST"))
        {
            SetRequestHeaders(r);
            await r.SendWebRequest();
            HandleError(r);
            if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
            {
                Debug.Log("HTTP response body: " + r.downloadHandler.text);
                return JsonConvert.DeserializeObject<VaultKeyList>(r.downloadHandler.text);
            }
            return new VaultKeyList
            {
                Keys = new string[] { }
            };
        }
    }

    public async Task<T> GetAsync<T>(string path)
    {
        using (var r = UnityWebRequest.Get(this.url + path))
        {
            SetRequestHeaders(r);
            await r.SendWebRequest();
            HandleError(r);
            Debug.Log("HTTP response body: " + r.downloadHandler.text);
            return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
        }
    }

    public async Task<T> PutAsync<T, U>(string path, U data)
    {
        string body = JsonConvert.SerializeObject(data);
        Debug.Log("PutAsync JSON body: " + body);
        byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
        using (var r = new UnityWebRequest(this.url + path, "POST"))
        {
            r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            SetRequestHeaders(r);
            await r.SendWebRequest();
            HandleError(r);
            Debug.Log("Response: " + r.downloadHandler.text);
            return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
        }
    }

    public async Task PutAsync<T>(string path, T data)
    {
        string body = JsonConvert.SerializeObject(data);
        Debug.Log("PutAsync JSON body: " + body);
        byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
        using (var r = new UnityWebRequest(this.url + path, "POST"))
        {
            r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            SetRequestHeaders(r);
            await r.SendWebRequest();
            HandleError(r);
            Debug.Log("Response: " + r.downloadHandler.text);
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
