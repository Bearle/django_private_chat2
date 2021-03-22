#!/usr/bin/env python
# -*- coding: utf-8 -*-

"""
test_django_private_chat2
------------

Tests for `django_private_chat2` models module.
"""

from django.test import TestCase

from django_private_chat2.models import DialogsModel, MessageModel
from django.forms.models import model_to_dict
from django.conf import settings
from .factories import DialogsModelFactory, MessageModelFactory, UserFactory


class MessageModelTests(TestCase):

    def setUp(self):
        print("tests work")

    def test_something(self):
        pass

    def tearDown(self):
        pass


class TestCaseDialogsModel(TestCase):

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


class TestCaseMessageModel(TestCase):
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
        self.assertIsNotNone(message_model.file)
        self.assertIsNotNone(message_model.read)
