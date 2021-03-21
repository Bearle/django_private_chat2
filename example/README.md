##Example Project for django_private_chat2

This example is provided as a convenience feature to allow potential users to try the app straight from the app repo without having to create a django project.

It can also be used to develop the app in place.

To run this example, follow these instructions:

1. Navigate to the directory with this README
2. Create virtualenv  - Optional

```bash
virtualenv venv -p python3.9
source venv/bin/activate
# venv\Scripts\activate on Windows
```

3. Install the requirements for the package:

`pip install -r requirements.txt`
		
4. Go up a folder
`cd ..`
   
5. Apply migrations
		
```bash
python manage.py migrate
python manage.py loaddata example/fixtures/db.json
```
		
6. Run the server

`python manage.py runserver`
		
7. Access from the browser at `http://127.0.0.1:8000`
   
   There are two example users: user1 and user2. They have the same password: pa$$word
