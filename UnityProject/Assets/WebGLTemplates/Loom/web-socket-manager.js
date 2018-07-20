'use strict';

function WebSocketManager() {
  this.sockets = [];
  this.nextSocketId = 1;
}
  
WebSocketManager.prototype.createSocket = function (callbacks) {
  const socket = {
    id: this.nextSocketId++,
    url: null,
    onOpen: callbacks.open,
    onClose: callbacks.close,
    onMsg: callbacks.msg,
    ws: null
  };
  this.sockets.push(socket);
  return socket.id;
}

WebSocketManager.prototype.connectSocket = function (socketId, url) {
  const socket = this.getSocket(socketId);
  socket.url = url;
  socket.ws = new WebSocket(url);
  socket.ws.binaryType = 'arraybuffer';
  socket.ws.onopen = () => {
    socket.onOpen(socketId);
    socket.ws.onopen = undefined;
  }
  socket.ws.onmessage = e => socket.onMsg(socketId, e.data);
  // NOTE: socket.ws.onerror doesn't actually get any info about the error when it is fired,
  // as such it is rather pointless to subscribe to this event.
  socket.ws.onclose = e => {
    let err = null;
    if (e.code != 1000)
    {
      if (e.reason != null && e.reason.length > 0) {
        err = e.reason;
      } else {
        switch (e.code) {
          case 1001: 
            err = "Endpoint going away.";
            break;
          case 1002: 
            err = "Protocol error.";
            break;
          case 1003: 
            err = "Unsupported message.";
            break;
          case 1005: 
            err = "No status.";
            break;
          case 1006: 
            err = "Abnormal disconnection.";
            break;
          case 1009: 
            err = "Data frame too large.";
            break;
          default:
            err = "Error " + e.code;
        }
      }
    }
    socket.onClose(socket.id, err);
  }
}

WebSocketManager.prototype.destroySocket = function (socketId) {
  const socket = this.getSocket(socketId);
  if (socket) {
    if (socket.ws) {
      socket.ws.close();
      socket.ws = null;
    }
    const idx = this.sockets.indexOf(socket);
    if (idx != -1) {
      this.sockets.splice(idx, 1);
    }
  }
}

WebSocketManager.prototype.getSocket = function (socketId) {
  for (var i = 0; i < this.sockets.length; i++) {
    if (this.sockets[i].id === socketId) {
      return this.sockets[i];
    }
  }
  return null;
}

WebSocketManager.prototype.getSocketState = function (socketId) {
  const socket = this.getSocket(socketId);
  if (socket && socket.ws) {
    return socket.ws.readyState;
  }
  return 3 /* Closed */;
}

WebSocketManager.prototype.send = function (socketId, msg) {
  this.getSocket(socketId).ws.send(msg);
}

window.LoomWebSocketManager = new WebSocketManager();
