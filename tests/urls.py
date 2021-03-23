from django.conf.urls import url, include

urlpatterns = [
    url(r'', include('django_private_chat2.urls', namespace='django_private_chat2')),
]
