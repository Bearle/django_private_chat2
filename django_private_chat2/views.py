# -*- coding: utf-8 -*-
from django.views.generic import (
    CreateView,
    DeleteView,
    DetailView,
    UpdateView,
    ListView
)

from .models import (
    Dialog,
    Message,
    MessageModel
)
from .serializers import serialize_message_model
from django.db.models import Q

from django.contrib.auth.mixins import LoginRequiredMixin

from django.http import HttpResponse, JsonResponse
from django.core.paginator import Page, Paginator
from django.conf import settings


class MessagesModelList(LoginRequiredMixin, ListView):
    http_method_names = ['get', ]
    paginate_by = getattr(settings,'MESSAGES_PAGINATION', 500)

    def get_queryset(self):
        qs = MessageModel.objects.filter(Q(recipient=self.request.user) |
                                         Q(sender=self.request.user))

        return qs.order_by('-created')

    def render_to_response(self, context, **response_kwargs):
        data = [serialize_message_model(i) for i in context['object_list']]
        page: Page = context.pop('page_obj')
        paginator: Paginator = context.pop('paginator')
        return_data = {
            'page': page.number,
            'pages': paginator.num_pages,
            'data': data
        }
        return JsonResponse(return_data, **response_kwargs)
