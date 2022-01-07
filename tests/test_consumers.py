from django.test import TestCase

from django_private_chat2.models import DialogsModel, MessageModel, UploadedFile
from django.db import IntegrityError
from .factories import DialogsModelFactory, MessageModelFactory, UserFactory, faker
from django.test import TestCase, Client
from django.urls import reverse, resolve
from django.conf import settings
from django.contrib.auth.models import AnonymousUser, User
from django_private_chat2.serializers import serialize_message_model, serialize_dialog_model
import json
from channels.testing import HttpCommunicator, WebsocketCommunicator
from channels.db import database_sync_to_async

from django_private_chat2.consumers import ChatConsumer
from django_private_chat2.consumers.db_operations import  get_groups_to_add, get_user_by_pk, get_file_by_id, \
    get_message_by_id, get_unread_count, mark_message_as_read, save_file_message, save_text_message


class ConsumerTests(TestCase):
    def setUp(self) -> None:
        self.u1, self.u2 = UserFactory.create(), UserFactory.create()
        self.dialog: DialogsModel = DialogsModelFactory.create(user1=self.u1, user2=self.u2)
        self.file: UploadedFile = UploadedFile.objects.create(uploaded_by=self.u1, file="LICENSE")
        self.msg: MessageModel = MessageModelFactory.create(sender=self.u1, recipient=self.u2)
        self.unread_msg: MessageModel = MessageModelFactory.create(sender=self.u1, recipient=self.u2, read=False)

        self.sender, self.recipient = UserFactory.create(), UserFactory.create()
        num_unread = faker.random.randint(1, 20)
        _ = MessageModelFactory.create_batch(num_unread, read=False, sender=self.sender, recipient=self.recipient)
        self.num_unread = num_unread

    async def test_groups_to_add(self):
        groups = await get_groups_to_add(self.u1)
        self.assertEqual({1, 2}, groups)
        groups2 = await get_groups_to_add(self.u2)
        self.assertEqual({2, 1}, groups2)

    async def test_get_user_by_pk(self):
        user = await get_user_by_pk("1000")
        self.assertIsNone(user)
        user = await get_user_by_pk(self.u1.id)
        self.assertEqual(user, self.u1)

    async def test_get_file_by_id(self):
        f = await get_file_by_id("1000")
        self.assertIsNone(f)
        f = await get_file_by_id(self.file.id)
        self.assertEqual(f, self.file)

    async def test_get_message_by_id(self):
        m = await get_message_by_id(999999)
        self.assertIsNone(m)
        m = await get_message_by_id(self.msg.id)
        t = (str(self.u2.pk), str(self.u1.pk))
        self.assertEqual(m, t)

    async def test_mark_message_as_read(self):
        self.assertFalse(self.unread_msg.read)
        await mark_message_as_read(self.unread_msg.id)
        await database_sync_to_async(self.unread_msg.refresh_from_db)()
        self.assertTrue(self.unread_msg.read)

    async def test_get_unread_count(self):
        count = await get_unread_count(self.sender, self.recipient)
        self.assertEqual(count, self.num_unread)

    async def test_save_x_message(self):
        msg = await save_text_message(text="text", from_=self.u1, to=self.u2)
        self.assertIsNotNone(msg)
        msg2 = await save_file_message(file=self.file, from_=self.u1, to=self.u2)
        self.assertIsNotNone(msg2)

    async def test_connect_basic(self):
        communicator = WebsocketCommunicator(ChatConsumer.as_asgi(), "/chat_ws")
        communicator.scope["user"] = self.u1
        connected, subprotocol = await communicator.connect()
        assert connected
