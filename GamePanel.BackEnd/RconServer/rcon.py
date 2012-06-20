from os import path as op

import tornado.web
import tornadio
import tornadio.router
import tornadio.server
import json

import SourceRcon

ROOT = op.normpath(op.dirname(__file__))

class SourceRconConnection(tornadio.SocketConnection):
    # def on_open(self, *args, **kwargs):
    server = None
	
    def on_message(self, message):
        if message['action'] == 'connect':
            self.server = SourceRcon.SourceRcon(message['host'], message['port'], message['password'])
            self.send(json.dumps({"action": "connected"}))
        elif message['action'] == 'rcon':
            self.send(json.dumps({'action': 'rcon', 'orginalCommand': message['command'], 'output': self.server.rcon(message['command'])}))
        
    def on_close(self):
        if self.server:
            self.server.disconnect()

#use the routes classmethod to build the correct resource
RconRouter = tornadio.get_router(SourceRconConnection)

#configure the Tornado application
application = tornado.web.Application(
    [RconRouter.route()],
    enabled_protocols = ['websocket',
                         'flashsocket',
                         'xhr-multipart',
                         'xhr-polling'],
    flash_policy_port = 843,
    flash_policy_file = op.join(ROOT, 'flashpolicy.xml'),
    socket_io_port = 8001
)

if __name__ == "__main__":
    import logging
    logging.getLogger().setLevel(logging.DEBUG)

    tornadio.server.SocketServer(application)
