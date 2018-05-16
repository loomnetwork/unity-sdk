// Bridges Loom.Unity3d.WebGL.WSRPCClient and the WebSocketManager in web-socket-manager.js
var WebSocketManagerLib = {
  // NOTE: WSML will be undefined if the Use pre-built Engine option is enabled!
  $WSML: {
    onOpen: null,
    onClose: null,
    onMsg: null,
    
    getManager: function () {
      return window['LoomWebSocketManager'];
    }
  },

  InitWebSocketManagerLib: function (openCallback, closeCallback, msgCallback) {
    WSML.onOpen = function (socketId) {
      Runtime.dynCall('vi', openCallback, [socketId]);
    };
    WSML.onClose = function (socketId, errStr) {
      const errPtr = errStr ? allocateStringBuffer(errStr) : null;
      Runtime.dynCall('vii', closeCallback, [socketId, errPtr]);
    };
    WSML.onMsg = function (socketId) {
      Runtime.dynCall('vi', msgCallback, [socketId]);
    };
  },

  WebSocketCreate: function ()
  {
    return WSML.getManager().createSocket({
      open: WSML.onOpen,
      close: WSML.onClose,
      msg: WSML.onMsg
    });
  },

  WebSocketConnect: function (socketId, urlStrPtr) {
    const urlStr = Pointer_stringify(urlStrPtr);
    WSML.getManager().connectSocket(socketId, urlStr);
  },
  
  GetWebSocketState: function (socketId) {
    return WSML.getManager().getSocketState(socketId);
  },
  
  WebSocketSend: function (socketId, msgStrPtr) {
    const msgStr = Pointer_stringify(msgStrPtr)
    WSML.getManager().send(socketId, msgStr);
  },
  
  GetWebSocketMessage: function (socketId) {
    const msgStr = WSML.getManager().getMessage(socketId);
    return (msgStr !== null) ? allocateStringBuffer(msgStr) : null;
  },
  
  WebSocketClose: function (socketId) {
    WSML.getManager().destroySocket(socketId);
  }
};
    
autoAddDeps(WebSocketManagerLib, '$WSML');
mergeInto(LibraryManager.library, WebSocketManagerLib);
