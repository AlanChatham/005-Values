var osc = require('node-osc'),
    io = require('socket.io').listen(8081);

var oscServer, oscClient;

// This gets triggered when socket.io starts a connection
io.sockets.on('connection', function (socket) {
  // config-OSC is a custom message that recieves an
  //  object obj from the client containing setup data for the
  //  OSC server and client
  socket.on("config-OSC", function (obj) {
    oscServer = new osc.Server(obj.server.port, obj.server.host);
    oscClient = new osc.Client(obj.client.host, obj.client.port);

    oscClient.send('/status', socket.sessionId + ' connected');
    
    // Register a callback for the OSC server, so when
    //  the OSC server recieves an OSC message, it 
    //  forwards that message to the web client
    oscServer.on('message', function(msg, rinfo) {
      console.log(msg, rinfo);
      socket.emit("message", msg);
    });
  });
  // Register a callback so when the Socket.IO server
  //  recieves a message, it forwards that via the OSC
  //  client to whatever OSC device is listening
  //  on the client port
  socket.on("message", function (obj) {
    oscClient.send(obj);
  });
});