from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
from channels.db import database_sync_to_async
from .models import MessageModel
from typing import List, Set, Awaitable, Optional, Dict, Tuple
from django.contrib.auth.models import AbstractUser
import logging
import json
import enum

logger = logging.getLogger('django_private_chat2.consumers')


class ErrorTypes(enum.IntEnum):
    MessageParsingError = 1


ErrorDescription = Tuple[ErrorTypes, str]


class MessageTypes(enum.IntEnum):
    WentOnline = 1
    WentOffline = 2
    TextMessage = 3
    FileMessage = 4
    IsTyping = 5
    MessageRead = 6
    ErrorOccured = 7


@database_sync_to_async
def get_groups_to_add(u: AbstractUser) -> Set[int]:
    l = MessageModel.get_dialogs_for_user(u)
    return set(list(sum(l, ())))


class ChatConsumer(AsyncWebsocketConsumer):
    async def connect(self):
        # TODO:
        # 1. Set user online
        # 2. Notify other users that the user went online
        # 3. Add the user to all groups where he has dialogs
        # Call self.scope["session"].save() on any changes to User
        if self.scope["user"].is_authenticated:
            self.user: AbstractUser = self.scope['user']
            self.group_name: str = str(self.user.pk)
            logger.info(f"Sending 'user_went_online' for user {self.user.pk}")
            await self.channel_layer.group_send(self.group_name, {"type": "user_went_online", "user_pk": self.user.pk})
            await self.accept()
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} connected, "
                        f"adding channel {self.channel_name} to {dialogs} dialog groups")
            for d in dialogs:  # type: int
                await self.channel_layer.group_add(str(d), self.channel_name)
        else:
            await self.close(code=4001)

    async def disconnect(self, close_code):
        # TODO:
        # Set user offline
        # Save user was_online
        # Notify other users that the user went offline
        if close_code != 4001 and getattr(self, 'user', None) is not None:
            await self.channel_layer.group_send(self.group_name, {"type": "user_went_offline", "user_pk": self.user.pk})
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} disconnected, removing channel {self.channel_name} from groups {dialogs}")
            for d in dialogs:
                await self.channel_layer.group_discard(str(d), self.channel_name)

    # Receive message from WebSocket
    async def receive(self, text_data=None, bytes_data=None):
        error: Optional[ErrorDescription] = None
        try:
            text_data_json = json.loads(text_data)
            logger.info(f"From {self.group_name} received '{text_data_json}")
            if not ('msg_type' in text_data_json):
                error = (ErrorTypes.MessageParsingError, "msg_type not present in json")
            else:
                msg_type = text_data_json['msg_type']
                if not isinstance(msg_type, int):
                    error = (ErrorTypes.MessageParsingError, "msg_type is not an int")
                else:
                    try:
                        msg_type_case: MessageTypes = MessageTypes(msg_type)
                    except ValueError as e:
                        error = (ErrorTypes.MessageParsingError, f"msg_type decoding error - {e}")
        except json.JSONDecodeError as e:
            error = (ErrorTypes.MessageParsingError, f"jsonDecodeError - {e}")
            # TODO: handle json decode error

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
