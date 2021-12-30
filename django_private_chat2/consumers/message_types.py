import enum

# TODO: add tx_id to distinguish errors for different transactions

try:
    from typing import TypedDict
except ImportError:
    TypedDict = dict


class MessageTypeTextMessage(TypedDict):
    text: str
    user_pk: str
    random_id: int


class MessageTypeMessageRead(TypedDict):
    user_pk: str
    message_id: int


class MessageTypeFileMessage(TypedDict):
    file_id: str
    user_pk: str
    random_id: int


class MessageTypes(enum.IntEnum):
    WentOnline = 1
    WentOffline = 2
    TextMessage = 3
    FileMessage = 4
    IsTyping = 5
    MessageRead = 6
    ErrorOccurred = 7
    MessageIdCreated = 8
    NewUnreadCount = 9
    TypingStopped = 10
