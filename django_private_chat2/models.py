# -*- coding: utf-8 -*-

from django.db import models
from django.conf import settings
from django.utils.translation import gettext_lazy as _
from django.utils.timezone import localtime
from model_utils.models import TimeStampedModel, SoftDeletableModel, SoftDeletableManager
from django.contrib.auth.models import AbstractBaseUser
from django.contrib.auth import get_user_model
from typing import Optional, Any
from django.db.models import Q
import uuid

UserModel: AbstractBaseUser = get_user_model()


def user_directory_path(instance, filename):
    # file will be uploaded to MEDIA_ROOT/user_<id>/<filename>
    return f"user_{instance.uploaded_by.pk}/{filename}"


class UploadedFile(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    uploaded_by = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("Uploaded_by"),
                                    related_name='+', db_index=True)
    file = models.FileField(verbose_name=_("File"), blank=False, null=False, upload_to=user_directory_path)
    upload_date = models.DateTimeField(auto_now_add=True, verbose_name=_("Upload date"))

    def __str__(self):
        return str(self.file.name)


class DialogsModel(TimeStampedModel):
    id = models.BigAutoField(primary_key=True, verbose_name=_("Id"))
    user1 = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("User1"),
                              related_name="+", db_index=True)
    user2 = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("User2"),
                              related_name="+", db_index=True)

    class Meta:
        unique_together = (('user1', 'user2'), ('user2', 'user1'))
        verbose_name = _("Dialog")
        verbose_name_plural = _("Dialogs")

    def __str__(self):
        return _("Dialog between ") + f"{self.user1_id}, {self.user2_id}"

    @staticmethod
    def dialog_exists(u1: AbstractBaseUser, u2: AbstractBaseUser) -> Optional[Any]:
        return DialogsModel.objects.filter(Q(user1=u1, user2=u2) | Q(user1=u2, user2=u1)).first()

    @staticmethod
    def create_if_not_exists(u1: AbstractBaseUser, u2: AbstractBaseUser):
        res = DialogsModel.dialog_exists(u1, u2)
        if not res:
            DialogsModel.objects.create(user1=u1, user2=u2)

    @staticmethod
    def get_dialogs_for_user(user: AbstractBaseUser):
        return DialogsModel.objects.filter(Q(user1=user) | Q(user2=user)).values_list('user1__pk', 'user2__pk')


class MessageModel(TimeStampedModel, SoftDeletableModel):
    id = models.BigAutoField(primary_key=True, verbose_name=_("Id"))
    sender = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("Author"),
                               related_name='from_user', db_index=True)
    recipient = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, verbose_name=_("Recipient"),
                                  related_name='to_user', db_index=True)
    text = models.TextField(verbose_name=_("Text"), blank=True)
    file = models.ForeignKey(UploadedFile, related_name='message', on_delete=models.DO_NOTHING,
                             verbose_name=_("File"), blank=True, null=True)

    read = models.BooleanField(verbose_name=_("Read"), default=False)
    all_objects = models.Manager()

    @staticmethod
    def get_unread_count_for_dialog_with_user(sender, recipient):
        return MessageModel.objects.filter(sender_id=sender, recipient_id=recipient, read=False).count()

    @staticmethod
    def get_last_message_for_dialog(sender, recipient):
        return MessageModel.objects.filter(
            Q(sender_id=sender, recipient_id=recipient) | Q(sender_id=recipient, recipient_id=sender)) \
            .select_related('sender', 'recipient').first()

    def __str__(self):
        return str(self.pk)

    def save(self, *args, **kwargs):
        super(MessageModel, self).save(*args, **kwargs)
        DialogsModel.create_if_not_exists(self.sender, self.recipient)

    class Meta:
        ordering = ('-created',)
        verbose_name = _("Message")
        verbose_name_plural = _("Messages")

# TODO:
# Possible features - update with pts
# was_online field for User (1to1 model)
# read_at - timestamp
