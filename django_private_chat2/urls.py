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
    path('messages/', views.MessagesModelList.as_view(), name='all_messages_list'),
    path('messages/<dialog_with>/', views.MessagesModelList.as_view(), name='messages_list'),
    path('dialogs/', views.DialogsModelList.as_view(), name='dialogs_list'),
    path('self/', views.SelfInfoView.as_view(), name='self_info')
]
