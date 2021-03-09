# -*- coding: utf-8 -*-
from django.conf.urls import url
from django.views.generic import TemplateView
from . import consumers
# from . import views


app_name = 'django_private_chat2'
websocket_urlpatterns = [
    url(r'^ws$', consumers.ChatConsumer),
]

urlpatterns = []
