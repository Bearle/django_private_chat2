from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
from django.contrib.auth.models import AbstractUser
import logging
import json
import enum

logger = logging.getLogger('django_private_chat2.consumers')


class MessageTypes(enum.IntEnum):
    WentOnline = 1
    WentOffline = 2
    TextMessage = 3
    FileMessage = 4
    IsTyping = 5
    MessageRead = 6


class ChatConsumer(AsyncWebsocketConsumer):
    async def connect(self):
        # TODO:
        # 1. Set user online
        # 2. Notify other users that the user went online
        # Call self.scope["session"].save() on any changes to User
        self.user: AbstractUser = self.scope["user"]
        if self.user.is_authenticated:
            self.group_name = str(self.user.pk)
            logger.info(f"User {self.user.pk} connected, adding to channel {self.channel_name}")

            await self.channel_layer.group_add(self.group_name, self.channel_name)
            logger.info(f"Sending 'user_went_online' for user {self.user.pk}")
            await self.channel_layer.group_send(self.group_name, {"type": "user_went_online", "user_pk": self.user.pk})
            await self.accept()
        else:
            await self.close()

    async def disconnect(self, close_code):
        pass
        # Set user offline
        # Save user was_online
        # Notify other users that the user went offline

    # Receive message from WebSocket
    async def receive(self, text_data=None, bytes_data=None):
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

    async def user_went_online(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.WentOnline,
                'user_pk': event['user_pk']
            }))


    async def recieve_group_message(self, event):
        message = event['message']

        # Send message to WebSocket
        await self.send(
            text_data=json.dumps({
                'message': message
            }))
