from django.urls import re_path, include

urlpatterns = [
    re_path(r'', include('django_private_chat2.urls', namespace='django_private_chat2')),
]
