=====
Usage
=====

To use django_private_chat2 in a project, add it to your `INSTALLED_APPS`:

.. code-block:: python

    INSTALLED_APPS = (
        ...
        'django_private_chat2.apps.DjangoPrivateChat2Config',
        ...
    )

Add django_private_chat2's URL patterns:

.. code-block:: python

    from django_private_chat2 import urls as django_private_chat2_urls


    urlpatterns = [
        ...
        url(r'^', include(django_private_chat2_urls)),
        ...
    ]
