=============================
django_private_chat2
=============================

.. image:: https://badge.fury.io/py/django_private_chat2.svg
    :target: https://badge.fury.io/py/django_private_chat2

.. image:: https://travis-ci.org/delneg/django_private_chat2.svg?branch=master
    :target: https://travis-ci.org/delneg/django_private_chat2

.. image:: https://codecov.io/gh/delneg/django_private_chat2/branch/master/graph/badge.svg
    :target: https://codecov.io/gh/delneg/django_private_chat2

Your project description goes here

Documentation
-------------

The full documentation is at https://django_private_chat2.readthedocs.io.

Quickstart
----------

Install django_private_chat2::

    pip install django_private_chat2

Add it to your `INSTALLED_APPS`:

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

Features
--------

* TODO

Running Tests
-------------

Does the code actually work?

::

    source <YOURVIRTUALENV>/bin/activate
    (myenv) $ pip install tox
    (myenv) $ tox


Development commands
---------------------

::

    pip install -r requirements_dev.txt
    invoke -l


Credits
-------

Tools used in rendering this package:

*  Cookiecutter_
*  `cookiecutter-djangopackage`_

.. _Cookiecutter: https://github.com/audreyr/cookiecutter
.. _`cookiecutter-djangopackage`: https://github.com/pydanny/cookiecutter-djangopackage
