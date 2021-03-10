# -*- coding: utf-8 -*-
from django.conf.urls import url
from django.urls import path
from . import consumers
from . import views


app_name = 'django_private_chat2'
websocket_urlpatterns = [
    url(r'^chat_ws$', consumers.ChatConsumer.as_asgi()),
]

urlpatterns = [
    path('messages/', views.MessagesModelList.as_view(), name='messages_list')
]
