Django Private Chat2
======================

![https://badge.fury.io/py/django_private_chat2](https://badge.fury.io/py/django_private_chat2.svg)

![https://github.com/Bearle/django_private_chat2/actions](https://github.com/Bearle/django_private_chat2/actions/workflows/test.yml/badge.svg?branch=master)

![https://codecov.io/gh/Bearle/django_private_chat2](https://codecov.io/gh/Bearle/django_private_chat2/branch/master/graph/badge.svg)

New and improved  https://github.com/Bearle/django-private-chat

Chat app for Django, powered by Django Channels, Websockets & Asyncio

![screenshot](https://github.com/Bearle/django_private_chat2/blob/master/screenshots/screen.jpg?raw=true)

Documentation
-------------

The full documentation **will be** at <https://django_private_chat2.readthedocs.io>.

Quickstart
----------

Install django_private_chat2:

    pip install django_private_chat2

Add it to your `INSTALLED_APPS`:

```python
INSTALLED_APPS = (
    ...
    'django_private_chat2.apps.DjangoPrivateChat2Config',
    ...
)
```

Add django_private_chat2's URL patterns:

```python
from django.urls import re_path, include


urlpatterns = [
    ...
    re_path(r'', include('django_private_chat2.urls', namespace='django_private_chat2')),
    ...
]
```

Add django_private_chat2's websocket URLs to your `asgi.py`:
```python

django_asgi_app = get_asgi_application()
from channels.routing import ProtocolTypeRouter, URLRouter
from channels.auth import AuthMiddlewareStack
from django_private_chat2 import urls
application = ProtocolTypeRouter({
    "http": django_asgi_app,
    "websocket": AuthMiddlewareStack(
        URLRouter(urls.websocket_urlpatterns)
    ),
})

```


**Important:**

django_private_chat2 doesn't provide any endpoint to fetch users (required to start new chat, for example)
It's up to you to do so. The example app does it in `urls.py` so feel free to copy the handler from there if you wish.

Support
--------

It's important for us to have `django_private_chat2` thoroughly tested.

Using github actions, we're able to have `django_private_chat2` tested against python3.6, python3.7, python3.8, python3.9, python3.10 with Django 3.0, Django 3.1,  Django 3.2, Django 4.0

You can view the builds here:

https://github.com/Bearle/django_private_chat2/actions

The builds are composed of officially supported Django & Python combinations.

Please file an issue if you have any problems with any combination of the above. 


Features
--------

__Django-related__

-:white_check_mark: Fully-functional example app

-:white_check_mark: Uses Django admin

-:white_check_mark: Supports pluggable User model (and accounts for non-integer primary keys, too)

-:white_check_mark: Doesn't require Django Rest Framework (views are based off django.views.generic)

-:white_check_mark: Configurable via settings

-:white_check_mark: Fully translatable 

-:white_check_mark: Uses Django storage system & FileField for file uploads (swappable)


__Functionality-related__

-:white_check_mark: Soft deletable messages

-:white_check_mark: Read/unread messages

-:white_check_mark: Random id (send message first, write to database later)

-:white_check_mark: Supports text & file messages

-:white_check_mark: Gracefully handles errors

-:white_check_mark: Support 'typing' statuses

-:white_check_mark: Upload the file first, send the message later (async uploads) - potential for file ref re-use later

... and more


Example app frontend features
-----------------------------

1. Auto reconnected websocket
2. Toasts about errors & events
3. Send text messages
4. Search for users
5. Create new chat with another user
6. Online/offline status
7. Realtime messaging via websocket
8. Last message
9. Auto-avatar (identicon) based on user id
10. Connection status display
11. `Typing...` status
12. Message delivery status (sent, received, waiting, etc.)
13. Message history
14. Persistent chat list
15. Read / unread messages
16. Unread messages counters (updates when new messages are received)
17. Send file messages (upload them to server)

TODO 
----

Frontend (example app) & backend

1. Pagination support on frontend
    1. For messages 
    2. For dialogs
2. Example app only - user list
    1. :white_check_mark: Endpoint
    2. :white_check_mark: UI
3. :white_check_mark: New dialog support
4. Online's fetching on initial load
5. Last message
    1. :white_check_mark: In fetch
    2. :white_check_mark: In new dialog
    3. :white_check_mark: On arriving message
6. :white_check_mark: Read / unread/ unread count
7. Last seen
8. Send photo
9. :white_check_mark: Send file
10. Reply to message
11. Delete message
12. Forward message
13. Search for dialog (username)
    1. :white_check_mark: Frontend (local)
    2. ~~Server based~~ - won't do, out of the scope of the project
14. :white_check_mark: Fake data generator (to test & bench) - done via factories in tests
15. Cache dialogs (get_groups_to_add) - ?
16. Move views to async views - ?
17. Add some sounds
    1. New message
    2. New dialog
    3. Sent message received db_id
18. Optimize /messages/ endpoint
19. :white_check_mark:Some tests
20. Full test coverage
21. Migration from v1 guide
22. Documentation
23. self-messaging (Saved messages)

Running Tests
-------------

Does the code actually work?

    source <YOURVIRTUALENV>/bin/activate
    (myenv) $ pip install tox
    (myenv) $ tox

Development commands
--------------------

    pip install -r requirements_dev.txt
    invoke -l

Credits
-------

Tools used in rendering this package:

-   [Cookiecutter](https://github.com/audreyr/cookiecutter)
-   [cookiecutter-djangopackage](https://github.com/pydanny/cookiecutter-djangopackage)

Paid support
------------

If you wish to have professional help from the authors of django_private_chat2, or simply hire Django experts to solve a particular problem,
please contact us via email `tech` **at** `bearle.ru` or `Bearle` in telegram
