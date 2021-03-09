# -*- coding: utf-8 -*-

from django.db import models
from django.conf import settings
from django.utils.translation import ugettext as _
from django.utils.timezone import localtime
from model_utils.models import TimeStampedModel, SoftDeletableModel
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


def user_directory_path(instance, filename):
    # file will be uploaded to MEDIA_ROOT/user_<id>/<filename>
    return f"user_{instance.sender.pk}/{filename}"


class MessageModel(TimeStampedModel, SoftDeletableModel):
    id = models.BigAutoField(primary_key=True, verbose_name=_("Id"))
    sender = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("Author"),
                               related_name='from_user', db_index=True)
    recipient = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("Recipient"),
                                  related_name='to_user', db_index=True)
    text = models.TextField(verbose_name=_("Text"), blank=True)
    file = models.FileField(verbose_name=_("File"), blank=True, upload_to=user_directory_path)

    read = models.BooleanField(verbose_name=_("Read"), default=False)
    all_objects = models.Manager()

    def get_create_localtime(self):
        return localtime(self.created)

    def __str__(self):
        return self.pk

    class Meta:
        ordering = ('-created',)
        verbose_name = _("Message")
        verbose_name_plural = _("Messages")
