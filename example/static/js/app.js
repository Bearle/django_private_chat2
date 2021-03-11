document.addEventListener("DOMContentLoaded", function () {
    var socket = new WebSocket(
        'ws://' + window.location.host +
        '/chat_ws')

    socket.onopen = function (e) {
        socket.send(JSON.stringify({"msg_type": 5}));
        socket.send(JSON.stringify({"msg_type": "a"}));
        socket.send(JSON.stringify({"msg_typee": 5}));
        socket.send("");
    }
    socket.onmessage = function (e) {
        console.log("websocket message: ")
        console.log(e.data)
    };
});
