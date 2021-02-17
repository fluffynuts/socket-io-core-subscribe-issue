const topicOffsets = {
};

class Server {
    constructor() {
        this.server = require("http").createServer();
    }

    start() {
        this.connectedClients = {};
        this.io = require("socket.io")(this.server);
        this.io.on("connection", this._onClientConnected.bind(this));
        this.server.listen(3300);
        console.log("- listening on port 3300 -");
    }

    _onClientConnected(socket) {
        console.log(`- client connected: ${socket.id} -`);
        this.connectedClients[socket.id] = socket;
        socket.on("/publish", this._onClientPublishRequest.bind(this, socket));
        socket.on("/subscribe", this._onClientSubscribeRequest.bind(this, socket));
    }

    _onClientPublishRequest(socket, data, callback) {
        console.log(`publish request from ${socket.id}`, data);
        if (typeof data === "string") {
            data = JSON.parse(data);
        }
        const topic = data.topic;
        if (!topic) {
            throw new Error("no topic");
        }
        const channel = data.channel;
        if (channel === undefined) {
            throw new Error("no channel");
        }
        topicOffsets[topic] = topicOffsets[topic] || [];
        topicOffsets[topic][channel] = topicOffsets[topic][channel] || 0;

        try {
            // TODO: push message to connected clients
            Object.keys(this.connectedClients).forEach(clientId => {
                const 
                    client = this.connectedClients[clientId],
                    sub = client.subscriptions[topic];
                if (!sub) {
                    return;
                }
                if (sub.channel !== channel && 
                    sub.channel !== 0 // 0 is the "global" channel: get all messages
                ) {
                    return;
                }
                if (sub.offset < topicOffsets[topic][channel]) {
                    return;
                }
                client.emit("/consume", {
                    subscriptionId: sub.subscriptionId,
                    messages: [ data ]
                }, result => {
                    console.log("received ack to consume", result);
                });
            });
            callback({ success: true });
        } catch (err) {
            console.warn("publish error", err);
            callback({ success: false, reason: err.toString() });
        } finally {
            topicOffsets[topic][channel]++;
        }
    }

    _onClientSubscribeRequest(
        socket,
        request,
        callback
    ) {
        const client = this.connectedClients[socket.id];
        if (!client) {
            console.error("unknown client", socket.id);
            if (callback) {
                callback({ success: false, reason: "unknown client" });
            }
            return;
        }
        client.subscriptions = client.subscriptions || {};
        client.subscriptions[request.topic] = {
            offset: request.offset || 0,
            channel: request.channel || 0,
            // note: this really should be done by the server, _not_ the client
            id: request.subscriptionId
        };
        callback({ success: true });
    }
}

const server = new Server();
if (process.env.MAX_LIFE !== undefined) {
    const maxLifeInSeconds = parseInt(process.env.MAX_LIFE);
    if (isNaN(maxLifeInSeconds)) {
        throw new Error("MAX_LIFE must be a numeric value");
    }
    console.log(`- will automatically stop in ${maxLifeInSeconds} seconds...`);
    setTimeout(() => {
        console.log("- exiting now -");
        process.exit(0);
    }, maxLifeInSeconds * 1000);
}
server.start();