# -*- coding: utf-8 -*-

from django.db import models
from django.conf import settings
from django.utils.translation import ugettext as _
# from model_utils.models import TimeStampedModel
import dataclasses
import uuid
import datetime
from typing import Optional


@dataclasses.dataclass(frozen=True)
class DialogUser:
    id: uuid
    was_online: Optional[datetime.datetime]
    is_online: bool = False


@dataclasses.dataclass(frozen=True)
class Dialog:
    id: str
    creator: DialogUser
    opponent: DialogUser

    def __eq__(self, other) -> bool:
        return (self.creator.id == other.opponent.id and self.opponent.id == other.creator.id) or (
            self.creator.id == other.creator.id and self.opponent.id == other.opponent.id)


@dataclasses.dataclass(frozen=True)
class Message:
    dialog_id: str
    msg_id: int
    data: bytes
    sent_by: DialogUser
    sent_at: datetime.datetime
    was_read: bool


@dataclasses.dataclass(frozen=True)
class TextMessage(Message):
    data: str
