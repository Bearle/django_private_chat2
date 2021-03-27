# -*- coding: utf-8 -*-
from __future__ import unicode_literals, absolute_import

from django.conf.urls import url, include
from django.urls import path
from django.contrib import admin
from django.views.generic import TemplateView, ListView, View
from django.views.decorators.csrf import csrf_exempt
from django.contrib.auth.decorators import login_required
from django.contrib.auth.mixins import LoginRequiredMixin
from django.contrib.auth import get_user_model, login

from django.http import JsonResponse
from django.contrib.auth.models import AbstractBaseUser

from typing import List

UserModel = get_user_model()


class UsersListView(LoginRequiredMixin, ListView):
    http_method_names = ['get', ]

    def get_queryset(self):
        return UserModel.objects.all().exclude(id=self.request.user.id)

    def render_to_response(self, context, **response_kwargs):
        users: List[AbstractBaseUser] = context['object_list']

        data = [{
            "username": user.get_username(),
            "pk": str(user.pk)
        } for user in users]
        return JsonResponse(data, safe=False, **response_kwargs)


class DumbAuth(View):
    http_method_names = ['post', ]

    def post(self, request, *args, **kwargs):
        username, password = request.POST['username'], request.POST['password']
        email = f"{username}@example.com"
        user = UserModel.objects.filter(username=username).first()
        if not user:
            user = UserModel.objects.create_user(username, email, password)
        u = login(request=request, user=user)
        if u:
            return JsonResponse({'ok': True})
        else:
            return JsonResponse({'ok': False})


urlpatterns = [
    url('admin/', admin.site.urls),
    url(r'', include('django_private_chat2.urls', namespace='django_private_chat2')),
    path('dumb_auth/', csrf_exempt(DumbAuth.as_view()), name='dumb_auth'),
    path('users/', UsersListView.as_view(), name='users_list'),
    path('', login_required(TemplateView.as_view(template_name='base.html')), name='home'),
]
