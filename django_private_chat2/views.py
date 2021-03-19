# -*- coding: utf-8 -*-
from django.views.generic import (
    CreateView,
    DeleteView,
    DetailView,
    UpdateView,
    ListView
)

from .models import (
    MessageModel,
    DialogsModel
)
from .serializers import serialize_message_model, serialize_dialog_model
from django.db.models import Q

from django.contrib.auth.mixins import LoginRequiredMixin

from django.http import HttpResponse, JsonResponse
from django.core.paginator import Page, Paginator
from django.conf import settings
from django.contrib.auth.models import AbstractBaseUser


class MessagesModelList(LoginRequiredMixin, ListView):
    http_method_names = ['get', ]
    paginate_by = getattr(settings, 'MESSAGES_PAGINATION', 500)

    def get_queryset(self):
        if self.kwargs.get('dialog_with'):
            qs = MessageModel.objects \
                .filter(Q(recipient=self.request.user, sender=self.kwargs['dialog_with']) |
                        Q(sender=self.request.user, recipient=self.kwargs['dialog_with'])) \
                .select_related('sender', 'recipient')
        else:
            qs = MessageModel.objects.filter(Q(recipient=self.request.user) |
                                             Q(sender=self.request.user)).prefetch_related('sender', 'recipient')

        return qs.order_by('-created')

    def render_to_response(self, context, **response_kwargs):
        user_pk = self.request.user.pk
        data = [serialize_message_model(i, user_pk) for i in context['object_list']]
        page: Page = context.pop('page_obj')
        paginator: Paginator = context.pop('paginator')
        return_data = {
            'page': page.number,
            'pages': paginator.num_pages,
            'data': data
        }
        return JsonResponse(return_data, **response_kwargs)


class DialogsModelList(LoginRequiredMixin, ListView):
    http_method_names = ['get', ]
    paginate_by = getattr(settings, 'DIALOGS_PAGINATION', 20)

    def get_queryset(self):
        qs = DialogsModel.objects.filter(Q(user1_id=self.request.user.pk) | Q(user2_id=self.request.user.pk)) \
            .select_related('user1', 'user2')
        return qs.order_by('-created')

    def render_to_response(self, context, **response_kwargs):
        # TODO: add online status
        user_pk = self.request.user.pk
        data = [serialize_dialog_model(i, user_pk) for i in context['object_list']]
        page: Page = context.pop('page_obj')
        paginator: Paginator = context.pop('paginator')
        return_data = {
            'page': page.number,
            'pages': paginator.num_pages,
            'data': data
        }
        return JsonResponse(return_data, **response_kwargs)


class SelfInfoView(LoginRequiredMixin, DetailView):
    def get_object(self, queryset=None):
        return self.request.user

    def render_to_response(self, context, **response_kwargs):
        user: AbstractBaseUser = context['object']
        data = {
            "username": user.get_username(),
            "pk": str(user.pk)
        }
        return JsonResponse(data, **response_kwargs)
