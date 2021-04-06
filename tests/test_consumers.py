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
from channels.testing import HttpCommunicator
from django_private_chat2.consumers import ChatConsumer, get_groups_to_add, get_user_by_pk, get_file_by_id


class ConsumerTests(TestCase):
    def setUp(self) -> None:
        self.u1, self.u2 = UserFactory.create(), UserFactory.create()
        self.dialog = DialogsModelFactory.create(user1=self.u1, user2=self.u2)
        self.file = UploadedFile.objects.create(uploaded_by=self.u1, file="LICENSE")

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


