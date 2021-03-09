from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
import json


class ChatConsumer(AsyncWebsocketConsumer):
    async def connect(self):
        user_id = self.scope["session"]["_auth_user_id"]


        # self.group_name = "{}".format(user_id)

        # TODO:
        # 1. Set user online
        # 2. Notify other users that the user went online
        # Call self.scope["session"].save() on any changes to User
        l: InMemoryChannelLayer= self.channel_layer
        await self.channel_layer.group_add(
            self.group_name,
            self.channel_name
        )

        await self.accept()

    async def disconnect(self, close_code):
        pass
        # Set user offline
        # Save user was_online
        # Notify other users that the user went offline


    # Receive message from WebSocket
    async def receive(self, text_data=None,bytes_data = None):
        text_data_json = json.loads(text_data)


        print(text_data_json)

        # message = text_data_json['message']
        #
        # # Send message to room group
        # await self.channel_layer.group_send(
        #     self.chat_group_name,
        #     {
        #         'type': 'recieve_group_message',
        #         'message': message
        #     }
        # )

    async def recieve_group_message(self, event):
        message = event['message']

        # Send message to WebSocket
        await self.send(
             text_data=json.dumps({
            'message': message
        }))
