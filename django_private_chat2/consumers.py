from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
from channels.db import database_sync_to_async
from .models import MessageModel
from typing import List
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


@database_sync_to_async
def get_groups_to_add(u: AbstractUser) -> List[str]:
    return list(MessageModel.get_dialogs_for_user(u))


class ChatConsumer(AsyncWebsocketConsumer):
    async def connect(self):
        # TODO:
        # 1. Set user online
        # 2. Notify other users that the user went online
        # 3. Add the user to all groups where he has dialogs
        # Call self.scope["session"].save() on any changes to User
        self.user: AbstractUser = self.scope["user"]
        if self.user.is_authenticated:
            self.group_name = str(self.user.pk)
            logger.info(f"Sending 'user_went_online' for user {self.user.pk}")
            await self.channel_layer.group_send(self.group_name, {"type": "user_went_online", "user_pk": self.user.pk})
            await self.accept()
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} connected, "
                        f"adding channel {self.channel_name} to {len(dialogs)} dialog groups")
            for d in dialogs:  # type: str
                await self.channel_layer.group_add(d, self.channel_name)
        else:
            await self.close()

    async def disconnect(self, close_code):
        logger.info(
            f"User {self.user.pk} disconnected, removing channel {self.channel_name} from group {self.group_name}")

        await self.channel_layer.group_discard(
            self.group_name,
            self.channel_name
        )
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
