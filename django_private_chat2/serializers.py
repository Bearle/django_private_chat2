from .models import MessageModel, DialogsModel
from datetime import datetime
from typing import Callable
from django.contrib.auth.models import AbstractBaseUser
import json


def serialize_message_model(m: MessageModel, user_id):
    sender_pk = m.sender.pk
    is_out = sender_pk == user_id
    # TODO: add forwards
    # TODO: add replies
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
    unread_count = MessageModel.get_unread_count_for_dialog_with_user(sender=other_user.pk, recipient=user_id)
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
