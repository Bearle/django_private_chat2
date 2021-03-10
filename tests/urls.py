# -*- coding: utf-8 -*-
from __future__ import unicode_literals, absolute_import

from django.conf.urls import url, include
from django.urls import path
from django.contrib import admin
from django.views.generic import TemplateView
from django.contrib.auth.decorators import login_required
urlpatterns = [
    url('admin/', admin.site.urls),
    url(r'', include('django_private_chat2.urls', namespace='django_private_chat2')),
    path('', login_required(TemplateView.as_view(template_name='base.html')), name='home'),
]
