from django.test import TestCase, Client
from django.urls import reverse, resolve
from django.conf import settings
from django.contrib.auth.models import AnonymousUser, User
from django_private_chat2.models import DialogsModel, MessageModel
from django_private_chat2.serializers import serialize_message_model, serialize_dialog_model
import json
from .factories import DialogsModelFactory, MessageModelFactory, UserFactory, faker


# /dialogs/       django_private_chat2.views.DialogsModelList     django_private_chat2:dialogs_list
# /messages/      django_private_chat2.views.MessagesModelList    django_private_chat2:all_messages_list
# /messages/<dialog_with>/        django_private_chat2.views.MessagesModelList    django_private_chat2:messages_list
# /self/  django_private_chat2.views.SelfInfoView django_private_chat2:self_info


class ViewsTests(TestCase):

    def setUp(self):
        self.client1 = Client()
        self.client2 = Client()
        self.user1 = User.objects.create_user(username='user1', email='test1@example.com', password='top_secret')
        self.user2 = User.objects.create_user(username='user2', email='test2@example.com', password='top_secret')
        self.client1.force_login(self.user1)
        self.client2.force_login(self.user2)

    def test_self_view(self):
        response = self.client1.get(reverse('django_private_chat2:self_info'), follow=True)
        content = json.loads(response.content)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(content, {"username": self.user1.username, "pk": str(self.user1.id)})

    def test_dialogs_view(self):
        dialogs = DialogsModelFactory.create_batch(200, user1=self.user1)

        response = self.client1.get(reverse('django_private_chat2:dialogs_list'), follow=True)
        content = json.loads(response.content)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(content['pages'], 200 / settings.DIALOGS_PAGINATION)
        self.assertEqual(len(content['data']), settings.DIALOGS_PAGINATION)
        d = content['data'][0]
        dialog = list(filter(lambda x: x.id == d['id'], dialogs))[0]
        self.assertEqual(d, serialize_dialog_model(dialog, self.user1.id))

    def test_messages_view(self):
        messages1 = MessageModelFactory.create_batch(250, sender=self.user1, recipient=self.user2)
        messages2 = MessageModelFactory.create_batch(250, sender=self.user2, recipient=self.user1)
        messages = messages1 + messages2
        response = self.client1.get(reverse('django_private_chat2:all_messages_list'), follow=True)
        content = json.loads(response.content)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(content['pages'], 500 / settings.MESSAGES_PAGINATION)
        self.assertEqual(len(content['data']), settings.MESSAGES_PAGINATION)

        response = self.client2.get(reverse('django_private_chat2:all_messages_list'), follow=True)
        content2 = json.loads(response.content)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(content2['pages'], 500 / settings.MESSAGES_PAGINATION)
        self.assertEqual(len(content2['data']), settings.MESSAGES_PAGINATION)

        for m in messages2:
            self.assertIn(serialize_message_model(m, self.user1.id), content['data'])
            self.assertIn(serialize_message_model(m, self.user2.id), content2['data'])

        # creating these messages to pollute the 'all' response
        user3 = User.objects.create_user(username='user3', email='test3@example.com', password='top_secret')

        MessageModelFactory.create_batch(250, sender=self.user1, recipient=user3)
        response = self.client1.get(reverse('django_private_chat2:messages_list',
                                            kwargs={"dialog_with": self.user2.id}),
                                    follow=True)
        content = json.loads(response.content)
        self.assertEqual(response.status_code, 200)
        for m in messages2:
            self.assertIn(serialize_message_model(m, self.user1.id), content['data'])

    def tearDown(self):
        pass
