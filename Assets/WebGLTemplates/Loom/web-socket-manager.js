'use strict';

function WebSocketManager() {
  this.sockets = [];
  this.nextSocketId = 1;
}
  
WebSocketManager.prototype.createSocket = function (openCallback, msgCallback) {
  const socket = {
    id: this.nextSocketId++,
    url: null,
    onOpen: openCallback,
    onMsg: msgCallback,
    ws: null,
    error: null,
    messages: []
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
  socket.ws.onmessage = e => {
    socket.messages.push(e.data);
    socket.onMsg(socketId);
  };
  socket.ws.onerror = e => {
    console.log('Error: ' + e);
  }
  socket.ws.onclose = e => {
    if (e.code != 1000)
    {
      if (e.reason != null && e.reason.length > 0) {
        socket.error = e.reason;
      } else {
        switch (e.code)
        {
          case 1001: 
            socket.error = "Endpoint going away.";
            break;
          case 1002: 
            socket.error = "Protocol error.";
            break;
          case 1003: 
            socket.error = "Unsupported message.";
            break;
          case 1005: 
            socket.error = "No status.";
            break;
          case 1006: 
            socket.error = "Abnormal disconnection.";
            break;
          case 1009: 
            socket.error = "Data frame too large.";
            break;
          default:
            socket.error = "Error " + e.code;
        }
      }
    }
  }
}

WebSocketManager.prototype.destroySocket = function (socketId) {
  const socket = this.getSocket(socketId);
  socket.ws.close();
  socket.ws = null;
  const idx = this.sockets.indexOf(socket);
  if (idx != -1) {
    this.sockets.splice(idx, 1);
  }
}

WebSocketManager.prototype.getSocket = function (socketId) {
  for (var i = 0; i < this.sockets.length; i++) {
    if (this.sockets[i].id === socketId) {
      return this.sockets[i];
    }
  }
}

WebSocketManager.prototype.getSocketState = function (socketId) {
  const socket = this.getSocket(socketId);
  return socket.ws ? socket.ws.readyState : 3 /* Closed */;
}

WebSocketManager.prototype.getSocketError = function (socketId) {
  this.getSocket(socketId).error;
}

WebSocketManager.prototype.send = function (socketId, msg) {
  this.getSocket(socketId).ws.send(msg);
}

/*
WebSocketManager.prototype.peekMessage = function (socketId) {
  const socket = this.getSocket(socketId);
  if (socket.messages.length === 0) {
    return null;
  }
  return socket.messages[0];
}

WebSocketManager.prototype.popMessage = function (socketId, maxSize) {
  const socket = this.getSocket(socketId);
  if (socket.messages.length === 0) {
      return null;
  }
  const msg = socket.messages[0];
  if (msg.length > bufferSize) {
    return null;
  }
  socket.messages = socket.messages.slice(1);
  return msg;
}
*/

WebSocketManager.prototype.getMessage = function (socketId) {
  const socket = this.getSocket(socketId);
  if (socket.messages.length === 0) {
    return null;
  }
  const msg = socket.messages[0];
  socket.messages = socket.messages.slice(1);
  return msg;
}

window.LoomWebSocketManager = new WebSocketManager();
