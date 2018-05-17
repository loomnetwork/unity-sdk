﻿using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;

namespace Loom.Unity3d
{
    internal class HTTPRPCClient : IRPCClient
    {
        private static readonly string LogTag = "Loom.HTTPRPCClient";

        private Uri url;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public HTTPRPCClient(string url)
        {
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
        }

        public void Dispose()
        {
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            string body = JsonConvert.SerializeObject(new JsonRpcRequest<U>(method, args, Guid.NewGuid().ToString()));
            Logger.Log(LogTag, "Body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.url.AbsoluteUri, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                this.HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    var respMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(r.downloadHandler.text);
                    if (respMsg.Error != null)
                    {
                        throw new Exception(String.Format(
                            "JSON-RPC Error {0} ({1}): {2}",
                            respMsg.Error.Code, respMsg.Error.Message, respMsg.Error.Data
                        ));
                    }
                    return respMsg.Result;
                }
            }
            return default(T);
        }

        private void HandleError(UnityWebRequest r)
        {
            if (r.isNetworkError)
            {
                throw new Exception(String.Format("HTTP '{0}' request to '{1}' failed", r.method, r.url));
            }
            else if (r.isHttpError)
            {
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    // TOOD: extract error message if any
                }
                throw new Exception(String.Format("HTTP Error {0}", r.responseCode));
            }
        }
    }
}
