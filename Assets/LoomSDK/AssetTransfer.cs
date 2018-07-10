using System;
using System.Threading.Tasks;

#if UNITY_WEBGL
using AOT;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
#endif

namespace Loom.Unity3d
{
    public class AssetTransfer
    {
#if UNITY_WEBGL
        private static TaskCompletionSource<object> assetTransferTCS;

        [DllImport("__Internal")]
        private static extern void LOOM_ATL_TransferAsset(
            string asset,
            Action<string> doneCallback,
            Action<string> errorCallback);

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnTransferComplete(string resultStr)
        {
            if (string.IsNullOrEmpty(resultStr))
            {
                assetTransferTCS.TrySetResult(null);
            }
            else
            {
                var result = JsonConvert.DeserializeObject(resultStr);
                assetTransferTCS.TrySetResult(result);
            }
            assetTransferTCS = null;
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnTransferError(string errorStr)
        {
            if (string.IsNullOrEmpty(errorStr))
            {
                assetTransferTCS.TrySetException(new AssetTransferException());
            }
            else
            {
                assetTransferTCS.TrySetException(new AssetTransferException(errorStr));
            }
            assetTransferTCS = null;
        }

        /// <summary>
        /// Transfers an arbitary asset from one chain to another.
        /// </summary>
        /// <param name="asset">User defined asset.</param>
        /// <returns>User defined transfer result.</returns>
        /// <remarks>This API is experimental and will likely change.</remarks>
        public static Task<object> TransferAsset(object asset)
        {
            if (assetTransferTCS != null)
            {
                throw new InvalidOperationException("Asset transfer already in progress!");
            }
            assetTransferTCS = new TaskCompletionSource<object>();
            var assetStr = JsonConvert.SerializeObject(asset);
            LOOM_ATL_TransferAsset(assetStr, OnTransferComplete, OnTransferError);
            return assetTransferTCS.Task;
        }
#else
        public static Task<object> TransferAsset(object asset)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
