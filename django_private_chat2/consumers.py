from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
from channels.db import database_sync_to_async
from .models import MessageModel, DialogsModel, UserModel
from typing import List, Set, Awaitable, Optional, Dict, Tuple, TypedDict
from django.contrib.auth.models import AbstractBaseUser
from django.conf import settings
import logging
import json
import enum

logger = logging.getLogger('django_private_chat2.consumers')
TEXT_MAX_LENGTH = getattr(settings, 'TEXT_MAX_LENGTH', 65535)


class ErrorTypes(enum.IntEnum):
    MessageParsingError = 1
    TextMessageInvalid = 2
    InvalidMessageReadId = 3
    InvalidUserPk = 4


ErrorDescription = Tuple[ErrorTypes, str]


# TODO: add tx_id to distinguish errors for different transactions

class MessageTypeTextMessage(TypedDict):
    text: str
    user_pk: str


class MessageTypes(enum.IntEnum):
    WentOnline = 1
    WentOffline = 2
    TextMessage = 3
    FileMessage = 4
    IsTyping = 5
    MessageRead = 6
    ErrorOccured = 7
    DialogCreated = 8


@database_sync_to_async
def get_groups_to_add(u: AbstractBaseUser) -> Set[int]:
    l = DialogsModel.get_dialogs_for_user(u)
    return set(list(sum(l, ())))


@database_sync_to_async
def get_user_by_pk(pk: str) -> Optional[AbstractBaseUser]:
    return UserModel.objects.filter(pk=pk).first()


@database_sync_to_async
def save_text_message(text: str, from_: AbstractBaseUser, to: AbstractBaseUser) -> bool:
    """
    @rtype: bool - indicates whether a new dialog was created
    """
    return MessageModel.objects.create(text=text, sender=from_, recipient=to)


class ChatConsumer(AsyncWebsocketConsumer):
    async def connect(self):
        # TODO:
        # 1. Set user online
        # 2. Notify other users that the user went online
        # 3. Add the user to all groups where he has dialogs
        # Call self.scope["session"].save() on any changes to User
        if self.scope["user"].is_authenticated:
            self.user: AbstractBaseUser = self.scope['user']
            self.group_name: str = str(self.user.pk)
            logger.info(f"Sending 'user_went_online' for user {self.user.pk}")
            await self.channel_layer.group_send(self.group_name, {"type": "user_went_online", "user_pk": self.user.pk})
            await self.accept()
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} connected, "
                        f"adding channel {self.channel_name} to {dialogs} dialog groups")
            for d in dialogs:  # type: int
                # if str(d) != self.group_name:
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

    async def handle_received_message(self, msg_type: MessageTypes, data: Dict[str, str]) -> Optional[ErrorDescription]:
        logger.info(f"Received message type {msg_type.name} from user {self.group_name} with data {data}")
        if msg_type == MessageTypes.WentOffline \
            or msg_type == MessageTypes.WentOnline \
            or msg_type == MessageTypes.ErrorOccured:
            logger.info(f"Ignoring message {msg_type.name}")
        else:
            if msg_type == MessageTypes.IsTyping:
                await self.channel_layer.group_send(self.group_name, {"type": "is_typing", "user_pk": self.user.pk})
                return None
            elif msg_type == MessageTypes.TextMessage:
                data: MessageTypeTextMessage
                if 'text' not in data:
                    return ErrorTypes.MessageParsingError, "'text' not present in data"
                elif 'user_pk' not in data:
                    return ErrorTypes.MessageParsingError, "'user_pk' not present in data"
                elif data['text'] == '':
                    return ErrorTypes.TextMessageInvalid, "'text' should not be blank"
                elif len(data['text']) > TEXT_MAX_LENGTH:
                    return ErrorTypes.TextMessageInvalid, "'text' is too long"
                elif not isinstance(data['text'], str):
                    return ErrorTypes.TextMessageInvalid, "'text' should be a string"
                elif not isinstance(data['user_pk'], str):
                    return ErrorTypes.InvalidUserPk, "'user_pk' should be a string"
                else:
                    text = data['text']
                    user_pk = data['user_pk']
                    # first we send data to channel layer to not perform any synchronous operations,
                    # and only after we do sync DB stuff
                    await self.channel_layer.group_send(user_pk, {"type": "new_text_message",
                                                                  "from": self.group_name, "text": text})

                    recipient: Optional[AbstractBaseUser] = await get_user_by_pk(user_pk)
                    if not recipient:
                        return ErrorTypes.InvalidUserPk, f"User with pk {user_pk} does not exist"
                    else:
                        dialog_created: bool = await save_text_message(text, from_=self.user, to=recipient)
                        if dialog_created:
                            await self.channel_layer.group_send(user_pk,
                                                                {"type": "new_dialog", "with": self.group_name})

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
                        error = await self.handle_received_message(msg_type_case, text_data_json)
                    except ValueError as e:
                        error = (ErrorTypes.MessageParsingError, f"msg_type decoding error - {e}")
        except json.JSONDecodeError as e:
            error = (ErrorTypes.MessageParsingError, f"jsonDecodeError - {e}")
        if error is not None:
            await self.send(text_data=json.dumps({
                'msg_type': MessageTypes.ErrorOccured,
                'error': error
            }))
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

    async def new_dialog(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.DialogCreated,
                'with': event['with']
            }))

    async def new_text_message(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.TextMessage,
                'from': event['from'],
                'text': event['text']
            }))

    async def is_typing(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.IsTyping,
                'user_pk': event['user_pk']
            }))

    async def user_went_online(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.WentOnline,
                'user_pk': event['user_pk']
            }))

    async def user_went_offline(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.WentOffline,
                'user_pk': event['user_pk']
            }))
