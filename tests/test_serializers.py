from django.test import TestCase

from django_private_chat2.models import DialogsModel, MessageModel
from django_private_chat2.serializers import serialize_message_model, serialize_dialog_model
from .factories import DialogsModelFactory, MessageModelFactory, UserFactory, faker


class SerializerTests(TestCase):

    def setUp(self):
        self.sender = UserFactory.create()
        self.recipient = UserFactory.create()
        self.message = MessageModel.objects.create(sender=self.sender, recipient=self.recipient,
                                                   text="testText", read=True)
        self.dialog = DialogsModel.objects.filter(user1=self.sender, user2=self.recipient).first()

    def test_serialize_message_model(self):
        serialized = serialize_message_model(self.message, self.sender.pk)
        #TODO: test file serialization
        o = {
            "id": self.message.id,
            "text": "testText",
            "sent": int(self.message.created.timestamp()),
            "edited": int(self.message.modified.timestamp()),
            "read": True,
            "file": None,
            "sender": str(self.sender.pk),
            "recipient": str(self.recipient.pk),
            "out": True,
            "sender_username": self.sender.username
        }
        self.assertEqual(serialized, o)

    def test_serialize_dialog_model(self):
        serialized = serialize_dialog_model(self.dialog, self.sender.pk)
        o = {
            "id": self.dialog.id,
            "created": int(self.dialog.created.timestamp()),
            "modified": int(self.dialog.modified.timestamp()),
            "other_user_id": str(self.recipient.id),
            "unread_count": 0,
            "username": self.recipient.username,
            "last_message": serialize_message_model(self.message, self.sender.pk)
        }
        self.assertEqual(serialized, o)

    def tearDown(self):
        pass
