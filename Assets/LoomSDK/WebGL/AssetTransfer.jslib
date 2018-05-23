// Bridges Loom.Unity3d.WebGL.AssetTransfer and the asset transfer function in the host page.
var LoomAssetTransferLib = {
  // Starts an asset transfer.
  // @param inputStrPtr JSON-encoded string containing info about the reward transfer.
  // @param doneCallback Function to invoke when the transfer is complete, the callback will receive
  //                     a JSON-encoded string containing the result of the transfer.
  // @param errCallback Function to invoke if an error is encountered, the callback will receive a
  //                    string containing the error info.
  LOOM_ATL_TransferAsset: function (inputStrPtr, doneCallback, errCallback) {
    const inputStr = Pointer_stringify(inputStrPtr);
    if (!window.LOOM_SETTINGS.transferAsset) {
      throw new Error('Missing window.LOOM_SETTINGS.transferAsset function!');
    }
    window.LOOM_SETTINGS.transferAsset(JSON.parse(inputStr))
    .then(function (result) {
      const outputStr = JSON.stringify(result);
      Runtime.dynCall('vi', doneCallback, [allocateStringBuffer(outputStr)]);
    })
    .catch(function (error) {
      const errStr = error.message || error.toString();
      Runtime.dynCall('vi', errCallback, [allocateStringBuffer(errStr)]);
    });
  },
};

mergeInto(LibraryManager.library, LoomAssetTransferLib);
