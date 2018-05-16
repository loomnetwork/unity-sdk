var WebSocketManagerLib = {
  // NOTE: webSocketManager will be undefined if the Use pre-built Engine option is enabled!
  $webSocketManager: function() {
    return window['LoomWebSocketManager']
  },

  WebSocketCreate: function (openCallback, msgCallback)
  {
    return webSocketManager().createSocket(
      function (socketId) {
        Runtime.dynCall('vi', openCallback, [socketId]);
      },
      function (socketId) {
        Runtime.dynCall('vi', msgCallback, [socketId]);
      }
    );
  },

  WebSocketConnect: function (socketId, urlStrPtr) {
    const urlStr = Pointer_stringify(urlStrPtr);
    webSocketManager().connectSocket(socketId, urlStr);
  },
  
  GetWebSocketState: function (socketId) {
    return webSocketManager().getSocketState(socketId);
  },
  
  WebSocketError: function (socketId) {
    return allocateStringBuffer(webSocketManager().getSocketError(socketId) || '');
  },
  
  WebSocketSend: function (socketId, msgStrPtr) {
    const msgStr = Pointer_stringify(msgStrPtr)
    webSocketManager().send(socketId, msgStr);
  },
  
  GetWebSocketMessage: function (socketId) {
    const msgStr = webSocketManager().getMessage(socketId);
    return allocateStringBuffer((msgStr !== null) ? msgStr : '');
  },
  
  WebSocketClose: function (socketId)
  {
    webSocketManager().destroySocket(socketId);
  }
};
    
autoAddDeps(WebSocketManagerLib, '$webSocketManager');
mergeInto(LibraryManager.library, WebSocketManagerLib);
    