from channels.generic.websocket import AsyncWebsocketConsumer
from channels.layers import InMemoryChannelLayer
from channels.db import database_sync_to_async
from .models import MessageModel, DialogsModel, UserModel
from typing import List, Set, Awaitable, Optional, Dict, Tuple
from django.contrib.auth.models import AbstractBaseUser
from django.conf import settings
import logging
import json
import enum
import sys

can_use_TypedDict = sys.version_info.major >=3 and sys.version_info.minor >= 8
if can_use_TypedDict:
    from typing import TypedDict
logger = logging.getLogger('django_private_chat2.consumers')
TEXT_MAX_LENGTH = getattr(settings, 'TEXT_MAX_LENGTH', 65535)


class ErrorTypes(enum.IntEnum):
    MessageParsingError = 1
    TextMessageInvalid = 2
    InvalidMessageReadId = 3
    InvalidUserPk = 4
    InvalidRandomId = 5


ErrorDescription = Tuple[ErrorTypes, str]


# TODO: add tx_id to distinguish errors for different transactions

if can_use_TypedDict:
    class MessageTypeTextMessage(TypedDict):
        text: str
        user_pk: str
        random_id: int


    class MessageTypeMessageRead(TypedDict):
        user_pk: str
        message_id: int


class MessageTypes(enum.IntEnum):
    WentOnline = 1
    WentOffline = 2
    TextMessage = 3
    FileMessage = 4
    IsTyping = 5
    MessageRead = 6
    ErrorOccured = 7
    MessageIdCreated = 8
    NewUnreadCount = 9


@database_sync_to_async
def get_groups_to_add(u: AbstractBaseUser) -> Set[int]:
    l = DialogsModel.get_dialogs_for_user(u)
    return set(list(sum(l, ())))


@database_sync_to_async
def get_user_by_pk(pk: str) -> Optional[AbstractBaseUser]:
    return UserModel.objects.filter(pk=pk).first()


@database_sync_to_async
def get_message_by_id(mid: int) -> Optional[Tuple[str, str]]:
    msg: Optional[MessageModel] = MessageModel.objects.filter(id=mid).first()
    if msg:
        return str(msg.recipient.pk), str(msg.sender.pk)
    else:
        return None


# @database_sync_to_async
# def mark_message_as_read(mid: int, sender_pk: str, recipient_pk: str):
#     return MessageModel.objects.filter(id__lte=mid,sender_id=sender_pk, recipient_id=recipient_pk).update(read=True)

@database_sync_to_async
def mark_message_as_read(mid: int):
    return MessageModel.objects.filter(id=mid).update(read=True)


@database_sync_to_async
def get_unread_count(sender, recipient) -> int:
    return int(MessageModel.get_unread_count_for_dialog_with_user(sender, recipient))


@database_sync_to_async
def save_text_message(text: str, from_: AbstractBaseUser, to: AbstractBaseUser) -> MessageModel:
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
            self.sender_username: str = self.user.get_username()
            logger.info(f"User {self.user.pk} connected, adding {self.channel_name} to {self.group_name}")
            await self.channel_layer.group_add(self.group_name, self.channel_name)
            await self.accept()
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} connected, sending 'user_went_online' to {dialogs} dialog groups")
            for d in dialogs:  # type: int
                if str(d) != self.group_name:
                    await self.channel_layer.group_send(str(d),
                                                        {"type": "user_went_online", "user_pk": str(self.user.pk)})
        else:
            await self.close(code=4001)

    async def disconnect(self, close_code):
        # TODO:
        # Set user offline
        # Save user was_online
        # Notify other users that the user went offline
        if close_code != 4001 and getattr(self, 'user', None) is not None:
            logger.info(
                f"User {self.user.pk} disconnected, removing channel {self.channel_name} from group {self.group_name}")
            await self.channel_layer.group_discard(self.group_name, self.channel_name)
            dialogs = await get_groups_to_add(self.user)
            logger.info(f"User {self.user.pk} disconnected, sending 'user_went_offline' to {dialogs} dialog groups")
            for d in dialogs:
                await self.channel_layer.group_send(str(d), {"type": "user_went_offline", "user_pk": str(self.user.pk)})

    async def handle_received_message(self, msg_type: MessageTypes, data: Dict[str, str]) -> Optional[ErrorDescription]:
        logger.info(f"Received message type {msg_type.name} from user {self.group_name} with data {data}")
        if msg_type == MessageTypes.WentOffline \
            or msg_type == MessageTypes.WentOnline \
            or msg_type == MessageTypes.MessageIdCreated \
            or msg_type == MessageTypes.ErrorOccured:
            logger.info(f"Ignoring message {msg_type.name}")
        else:
            if msg_type == MessageTypes.IsTyping:
                dialogs = await get_groups_to_add(self.user)
                logger.info(f"User {self.user.pk} is typing, sending 'is_typing' to {dialogs} dialog groups")
                for d in dialogs:
                    if str(d) != self.group_name:
                        await self.channel_layer.group_send(str(d), {"type": "is_typing",
                                                                     "user_pk": str(self.user.pk)})
                return None
            elif msg_type == MessageTypes.MessageRead:
                if can_use_TypedDict:
                    data: MessageTypeMessageRead
                if 'user_pk' not in data:
                    return ErrorTypes.MessageParsingError, "'user_pk' not present in data"
                elif 'message_id' not in data:
                    return ErrorTypes.MessageParsingError, "'message_id' not present in data"
                elif not isinstance(data['user_pk'], str):
                    return ErrorTypes.InvalidUserPk, "'user_pk' should be a string"
                elif not isinstance(data['message_id'], int):
                    return ErrorTypes.InvalidRandomId, "'message_id' should be an int"
                elif data['message_id'] <= 0:
                    return ErrorTypes.InvalidMessageReadId, "'message_id' should be > 0"
                elif data['user_pk'] == self.group_name:
                    return ErrorTypes.InvalidUserPk, "'user_pk' can't be self  (you can't mark self messages as read)"
                else:
                    user_pk = data['user_pk']
                    mid = data['message_id']
                    logger.info(
                        f"Validation passed, marking msg from {user_pk} to {self.group_name} with id {mid} as read")
                    await self.channel_layer.group_send(user_pk, {"type": "message_read",
                                                                  "message_id": mid,
                                                                  "sender": user_pk,
                                                                  "receiver": self.group_name})
                    recipient: Optional[AbstractBaseUser] = await get_user_by_pk(user_pk)
                    logger.info(f"DB check if user {user_pk} exists resulted in {recipient}")
                    if not recipient:
                        return ErrorTypes.InvalidUserPk, f"User with pk {user_pk} does not exist"
                    else:
                        msg_res: Optional[Tuple[str, str]] = await get_message_by_id(mid)
                        if not msg_res:
                            return ErrorTypes.InvalidMessageReadId, f"Message with id {mid} does not exist"
                        elif msg_res[0] != self.group_name or msg_res[1] != user_pk:
                            return ErrorTypes.InvalidMessageReadId, f"Message with id {mid} was not sent by {user_pk} to {self.group_name}"
                        else:
                            await mark_message_as_read(mid)
                            new_unreads = await get_unread_count(user_pk, self.group_name)
                            await self.channel_layer.group_send(self.group_name,
                                                                {"type": "new_unread_count", "sender": user_pk,
                                                                 "unread_count": new_unreads})
                            # await mark_message_as_read(mid, sender_pk=user_pk, recipient_pk=self.group_name)

                return None
            elif msg_type == MessageTypes.TextMessage:
                if can_use_TypedDict:
                    data: MessageTypeTextMessage
                if 'text' not in data:
                    return ErrorTypes.MessageParsingError, "'text' not present in data"
                elif 'user_pk' not in data:
                    return ErrorTypes.MessageParsingError, "'user_pk' not present in data"
                elif 'random_id' not in data:
                    return ErrorTypes.MessageParsingError, "'random_id' not present in data"
                elif data['text'] == '':
                    return ErrorTypes.TextMessageInvalid, "'text' should not be blank"
                elif len(data['text']) > TEXT_MAX_LENGTH:
                    return ErrorTypes.TextMessageInvalid, "'text' is too long"
                elif not isinstance(data['text'], str):
                    return ErrorTypes.TextMessageInvalid, "'text' should be a string"
                elif not isinstance(data['user_pk'], str):
                    return ErrorTypes.InvalidUserPk, "'user_pk' should be a string"
                elif not isinstance(data['random_id'], int):
                    return ErrorTypes.InvalidRandomId, "'random_id' should be an int"
                elif data['random_id'] > 0:
                    return ErrorTypes.InvalidRandomId, "'random_id' should be negative"
                else:
                    text = data['text']
                    user_pk = data['user_pk']
                    rid = data['random_id']
                    # first we send data to channel layer to not perform any synchronous operations,
                    # and only after we do sync DB stuff
                    # We need to create a 'random id' - a temporary id for the message, which is not yet
                    # saved to the database. I.e. for the client it is 'pending delivery' and can be
                    # considered delivered only when it's saved to database and received a proper id,
                    # which is then broadcast separately both to sender & receiver.
                    logger.info(f"Validation passed, sending text message from {self.group_name} to {user_pk}")
                    await self.channel_layer.group_send(user_pk, {"type": "new_text_message",
                                                                  "random_id": rid,
                                                                  "text": text,
                                                                  "sender": self.group_name,
                                                                  "receiver": user_pk,
                                                                  "sender_username": self.sender_username})

                    recipient: Optional[AbstractBaseUser] = await get_user_by_pk(user_pk)
                    logger.info(f"DB check if user {user_pk} exists resulted in {recipient}")
                    if not recipient:
                        return ErrorTypes.InvalidUserPk, f"User with pk {user_pk} does not exist"
                    else:
                        logger.info(f"Will save text message from {self.user} to {recipient}")
                        msg = await save_text_message(text, from_=self.user, to=recipient)
                        ev = {"type": "message_id_created", "random_id": rid, "db_id": msg.id}
                        logger.info(f"Message with id {msg.id} saved, firing events to {user_pk} & {self.group_name}")
                        await self.channel_layer.group_send(user_pk, ev)
                        await self.channel_layer.group_send(self.group_name, ev)
                        new_unreads = await get_unread_count(self.group_name, user_pk)
                        await self.channel_layer.group_send(user_pk,
                                                            {"type": "new_unread_count", "sender": self.group_name,
                                                             "unread_count": new_unreads})

    # Receive message from WebSocket
    async def receive(self, text_data=None, bytes_data=None):
        logger.info(f"Receive fired")
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
            error_data = {
                'msg_type': MessageTypes.ErrorOccured,
                'error': error
            }
            logger.info(f"Will send error {error_data} to {self.group_name}")
            await self.send(text_data=json.dumps(error_data))
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

    async def new_unread_count(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.NewUnreadCount,
                'sender': event['sender'],
                'unread_count': event['unread_count']
            }))

    async def message_read(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.MessageRead,
                'message_id': event['message_id'],
                'sender': event['sender'],
                'receiver': event['receiver']
            }))

    async def message_id_created(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.MessageIdCreated,
                'random_id': event['random_id'],
                'db_id': event['db_id']
            }))

    async def new_text_message(self, event):
        await self.send(
            text_data=json.dumps({
                'msg_type': MessageTypes.TextMessage,
                "random_id": event['random_id'],
                "text": event['text'],
                "sender": event['sender'],
                "receiver": event['receiver'],
                "sender_username": event['sender_username'],
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
