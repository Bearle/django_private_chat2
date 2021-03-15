Django Private Chat2
======================

![image](https://badge.fury.io/py/django_private_chat2.svg%0A%20:target:%20https://badge.fury.io/py/django_private_chat2)

![image](https://travis-ci.org/delneg/django_private_chat2.svg?branch=master%0A%20:target:%20https://travis-ci.org/delneg/django_private_chat2)

![image](https://codecov.io/gh/delneg/django_private_chat2/branch/master/graph/badge.svg%0A%20:target:%20https://codecov.io/gh/delneg/django_private_chat2)

New and improved  https://github.com/Bearle/django-private-chat

Chat app for Django, powered by Django Channels, Websockets & Asyncio


Documentation
-------------

The full documentation is at <https://django_private_chat2.readthedocs.io>.

Quickstart
----------

Install django\_private\_chat2:

    pip install django_private_chat2

Add it to your \`INSTALLED\_APPS\`:

``` {.sourceCode .python}
INSTALLED_APPS = (
    ...
    'django_private_chat2.apps.DjangoPrivateChat2Config',
    ...
)
```

Add django\_private\_chat2's URL patterns:

``` {.sourceCode .python}
from django_private_chat2 import urls as django_private_chat2_urls


urlpatterns = [
    ...
    url(r'^', include(django_private_chat2_urls)),
    ...
]
```

Features
--------

__Django-related__

-:white_check_mark: Fully-functional example app

-:white_check_mark: Uses Django admin

-:white_check_mark: Supports pluggable User model (and accounts for non-integer primary keys, too)

-:white_check_mark: Doesn't require Django Rest Framework (views are based off django.views.generic)

-:white_check_mark: Configurable via settings

-:white_check_mark: Fully translatable 

__Functionality-related__

-:white_check_mark: Soft deletable messages

-:white_check_mark: Read/unread messages

-:white_check_mark: Random id (send message first, write to database later)

-:white_check_mark: Supports text & file messages

-:white_check_mark: Gracefully handles errors

-:white_check_mark: Support 'typing' statuses

... and more

TODO 
----

Frontend (example app) & backend

1. Pagination support
    1. For messages 
    2. For dialogs
2. New dialog support
3. Onlines fetching on initial load
4. Last message
    1. In fetch
    2. In new dialog
    3. On arriving message
5. Read / unread/ unread count
6. Last seen
7. Send photo
8. Send file
9. Reply to message
10. Delete message
11. Forward message
12. Search for dialog (username)
    1. + Frontend (local)
    2. Server based
13. Fake data generator (to test & bench)
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

