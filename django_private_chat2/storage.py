from django.core.cache import cache
from .models import Message, Dialog, DialogUser
from .serializers import serialize_dialog
from asgiref.sync import sync_to_async
from typing import Optional, Dict
import uuid


def get_dialog_id(uid1: uuid, uid2: uuid) -> str:
    return f"dialog_{uid1.hex + uid2.hex if uid1.hex > uid2.hex else uid2.hex + uid1.hex}"


@sync_to_async(thread_sensitive=False)
async def create_new_dialog(user1: DialogUser, user2: DialogUser) -> str:
    dialog_id: str = get_dialog_id(user1.id,user2.id)
    dialog_in_cache: Optional[str] = cache.get(dialog_id)
    if not dialog_in_cache:
        dialog = Dialog(id=dialog_id,creator=user1,opponent=user2)
        serialized: str = serialize_dialog(dialog)
        cache.set(dialog_id, serialized)
        return serialized
    else:
        return dialog_in_cache
