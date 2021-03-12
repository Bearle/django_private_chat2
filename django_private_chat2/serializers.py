from .models import DialogUser, Dialog, Message, TextMessage, MessageModel, DialogsModel
from datetime import datetime
from typing import Callable
from django.contrib.auth.models import AbstractBaseUser
import json
import dataclasses


def serialize_dialog_user(d: DialogUser, datetime_formatter: Callable[[datetime], str]) -> str:
    obj = {"id": d.id, "was_online": datetime_formatter(d.was_online) if d.was_online else None,
           "is_online": d.is_online}
    return json.dumps(obj)


def serialize_dialog(d: Dialog) -> str:
    return json.dumps({"id": d.id, "between": (d.creator.id, d.opponent.id)})


def serialize_text_message(m: TextMessage, datetime_formatter: Callable[[datetime], str]) -> str:
    obj = {
        "dialog_id": m.dialog_id,
        "data": m.data,
        "msg_id": m.msg_id,
        "sent_by": m.sent_by.id,
        "sent_at": datetime_formatter(m.sent_at),
        "was_read": m.was_read
    }
    return json.dumps(obj)


def serialize_message_model(m: MessageModel, user_id):
    sender_pk = m.sender.pk
    is_out = sender_pk == user_id
    #TODO: add forwards
    #TODO: add replies
    obj = {
        "id": m.id,
        "text": m.text,
        "sent": int(m.created.timestamp()),
        "edited": int(m.modified.timestamp()),
        "read": m.read,
        "file": m.file.path if m.file else None,
        "sender": str(sender_pk),
        "recipient": str(m.recipient.pk),
        "out": is_out,
        "sender_username": m.sender.get_username()
    }
    return obj


def serialize_dialog_model(m: DialogsModel, user_id):
    other_user: AbstractBaseUser = m.user1 if m.user2.pk == user_id else m.user2
    unread_count = MessageModel.get_unread_count_for_dialog_with_user(sender=other_user.pk,recipient=user_id)
    # TODO: add last message
    obj = {
        "id": m.id,
        "created": int(m.created.timestamp()),
        "modified": int(m.modified.timestamp()),
        "other_user_id": str(other_user.pk),
        "unread_count": unread_count,
        "username": other_user.get_username()
    }
    return obj
