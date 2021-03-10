document.addEventListener("DOMContentLoaded", function() {
  var socket = new WebSocket(
        'ws://' + window.location.host +
        '/chat_ws')


    socket.onmessage = function (e) {
        console.log("websocket message: ")
        console.log(e.data)
    };
});
