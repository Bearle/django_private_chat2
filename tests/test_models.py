#!/usr/bin/env python
# -*- coding: utf-8 -*-

"""
test_django_private_chat2
------------

Tests for `django_private_chat2` models module.
"""

from django.test import TestCase

from django_private_chat2.models import DialogsModel, MessageModel, UploadedFile
from django.forms.models import model_to_dict

from django.db import IntegrityError
from .factories import DialogsModelFactory, MessageModelFactory, UserFactory, faker


class UploadedFileModelTests(TestCase):
    def setUp(self) -> None:
        self.file = UploadedFile.objects.create(uploaded_by=UserFactory.create(), file="LICENSE")

    def test_str(self):
        self.assertEqual(str(self.file), "LICENSE")


class MessageAndDialogModelTests(TestCase):

    def setUp(self):
        UserFactory.create_batch(10)
        self.msg1 = MessageModelFactory.create()

    def test_str_message(self):
        self.assertEqual(str(self.msg1), str(self.msg1.pk))

    def test_str_dialog(self):
        u1, u2 = UserFactory.create(), UserFactory.create()
        dialog = DialogsModelFactory.create(user1=u1, user2=u2)
        self.assertEqual(str(dialog), f"Dialog between {u1.pk}, {u2.pk}")

    def test_dialog_unique(self):
        u1, u2 = UserFactory.create(), UserFactory.create()
        DialogsModelFactory.create(user1=u1, user2=u2)
        DialogsModelFactory.create(user1=u2, user2=u1)
        self.assertRaises(IntegrityError, DialogsModelFactory.create, user2=u1, user1=u2)

    def test_get_dialogs_for_user(self):
        u1, u2 = UserFactory.create(), UserFactory.create()
        DialogsModelFactory.create(user1=u1, user2=u2)
        d = DialogsModel.get_dialogs_for_user(user=u1).first()
        d2 = DialogsModel.get_dialogs_for_user(user=u2).first()
        self.assertEqual(d, d2)

    def test_get_unread_count_for_dialog_with_user(self):
        sender, recipient = UserFactory.create(), UserFactory.create()
        num_unread = faker.random.randint(1, 20)
        _ = MessageModelFactory.create_batch(num_unread, read=False, sender=sender, recipient=recipient)

        self.assertEqual(MessageModel.get_unread_count_for_dialog_with_user(sender, recipient), num_unread)

    def test_get_last_message_for_dialog(self):
        sender, recipient = UserFactory.create(), UserFactory.create()
        last_message = MessageModelFactory.create(sender=sender, recipient=recipient)

        last_message1 = MessageModel.get_last_message_for_dialog(sender, recipient)
        last_message2 = MessageModel.get_last_message_for_dialog(recipient, sender)

        self.assertEqual(last_message, last_message1)
        self.assertEqual(last_message, last_message2)

    def test_save_creates_dialog_if_not_exists(self):
        before = DialogsModel.objects.count()
        sender, recipient = UserFactory.create(), UserFactory.create()
        MessageModelFactory.create(sender=sender, recipient=recipient)
        after = DialogsModel.objects.count()
        self.assertEqual(after, before + 1)

    def tearDown(self):
        pass


class TestCaseDialogsModelGenerated(TestCase):

    def test_create(self):
        """
        Test the creation of a DialogsModel model using a factory
        """
        dialogs_model = DialogsModelFactory.create()
        self.assertEqual(DialogsModel.objects.count(), 1)

    def test_create_batch(self):
        """
        Test the creation of 5 DialogsModel models using a factory
        """
        dialogs_models = DialogsModelFactory.create_batch(5)
        self.assertEqual(DialogsModel.objects.count(), 5)
        self.assertEqual(len(dialogs_models), 5)

    def test_attribute_count(self):
        """
        Test that all attributes of DialogsModel server are counted. It will count the primary key and all editable attributes.
        This test should break if a new attribute is added.
        """
        dialogs_model = DialogsModelFactory.create()
        dialogs_model_dict = model_to_dict(dialogs_model)
        self.assertEqual(len(dialogs_model_dict.keys()), 3)

    def test_attribute_content(self):
        """
        Test that all attributes of DialogsModel server have content. This test will break if an attributes name is changed.
        """
        dialogs_model = DialogsModelFactory.create()
        self.assertIsNotNone(dialogs_model.created)
        self.assertIsNotNone(dialogs_model.modified)
        self.assertIsNotNone(dialogs_model.id)
        self.assertIsNotNone(dialogs_model.user1)
        self.assertIsNotNone(dialogs_model.user2)


class TestCaseMessageModelGenerated(TestCase):
    def setUp(self):
        UserFactory.create_batch(15)

    def test_create(self):
        """
        Test the creation of a MessageModel model using a factory
        """

        message_model = MessageModelFactory.create()

        self.assertEqual(MessageModel.objects.count(), 1)

    def test_create_batch(self):
        """
        Test the creation of 5 MessageModel models using a factory
        """
        message_models = MessageModelFactory.create_batch(5)
        self.assertEqual(MessageModel.objects.count(), 5)
        self.assertEqual(len(message_models), 5)

    def test_attribute_count(self):
        """
        Test that all attributes of MessageModel server are counted. It will count the primary key and all editable attributes.
        This test should break if a new attribute is added.
        """
        message_model = MessageModelFactory.create()
        message_model_dict = model_to_dict(message_model)
        self.assertEqual(len(message_model_dict.keys()), 7)

    def test_attribute_content(self):
        """
        Test that all attributes of MessageModel server have content. This test will break if an attributes name is changed.
        """
        message_model = MessageModelFactory.create()
        self.assertIsNotNone(message_model.created)
        self.assertIsNotNone(message_model.modified)
        self.assertIsNotNone(message_model.is_removed)
        self.assertIsNotNone(message_model.id)
        self.assertIsNotNone(message_model.sender)
        self.assertIsNotNone(message_model.recipient)
        self.assertIsNotNone(message_model.text)
        self.assertIsNone(message_model.file)
        self.assertIsNotNone(message_model.read)
