from channels.db import database_sync_to_async
from django_private_chat2.models import MessageModel, DialogsModel, UserModel, UploadedFile
from typing import Set, Awaitable, Optional, Tuple
from django.contrib.auth.models import AbstractBaseUser
from django.core.exceptions import ValidationError


@database_sync_to_async
def get_groups_to_add(u: AbstractBaseUser) -> Awaitable[Set[int]]:
    l = DialogsModel.get_dialogs_for_user(u)
    return set(list(sum(l, ())))


@database_sync_to_async
def get_user_by_pk(pk: str) -> Awaitable[Optional[AbstractBaseUser]]:
    return UserModel.objects.filter(pk=pk).first()


@database_sync_to_async
def get_file_by_id(file_id: str) -> Awaitable[Optional[UploadedFile]]:
    try:
        f = UploadedFile.objects.filter(id=file_id).first()
    except ValidationError:
        f = None
    return f


@database_sync_to_async
def get_message_by_id(mid: int) -> Awaitable[Optional[Tuple[str, str]]]:
    msg: Optional[MessageModel] = MessageModel.objects.filter(id=mid).first()
    if msg:
        return str(msg.recipient.pk), str(msg.sender.pk)
    else:
        return None


# @database_sync_to_async
# def mark_message_as_read(mid: int, sender_pk: str, recipient_pk: str):
#     return MessageModel.objects.filter(id__lte=mid,sender_id=sender_pk, recipient_id=recipient_pk).update(read=True)

@database_sync_to_async
def mark_message_as_read(mid: int) -> Awaitable[None]:
    return MessageModel.objects.filter(id=mid).update(read=True)


@database_sync_to_async
def get_unread_count(sender, recipient) -> Awaitable[int]:
    return int(MessageModel.get_unread_count_for_dialog_with_user(sender, recipient))


@database_sync_to_async
def save_text_message(text: str, from_: AbstractBaseUser, to: AbstractBaseUser) -> Awaitable[MessageModel]:
    return MessageModel.objects.create(text=text, sender=from_, recipient=to)


@database_sync_to_async
def save_file_message(file: UploadedFile, from_: AbstractBaseUser, to: AbstractBaseUser) -> Awaitable[MessageModel]:
    return MessageModel.objects.create(file=file, sender=from_, recipient=to)
