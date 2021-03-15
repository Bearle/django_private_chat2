# DjangoPrivateChat2 Example app 

This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).


## Description:

The app is React.js SPA with business logic written in F#+Fable, compiled to JS and used from App.js as imported functions.

## Preface:

In order to run the app, you won't need this app explicitely.
The app is built & shipped via main.css & main.js in the folder above (in static) and is used by Django in base.html template. 


If you wish to build it yourself (or for development), read below.

## Pre-requisites

1.[Yarn](https://yarnpkg.com/) 

`npm install -g yarn`
2. [dotnet 3.1 or later SDK (dotnet 5 sdk preferably)](https://dotnet.microsoft.com/download/dotnet/5.0)
   
3. Install dependencies (fable dotnet tool will be auto-installed)

`cd frontend && yarn install`
   
## Build & development

Production - `npm run build`

For development I personally run 3 terminals 
- `python manage.py runserver` for Django
- `npm run start:fable` for F# + Fable
- `npm run build:jsonly` for React.js

That way I can change each part separately.

ATM, webpack dev server is not configured (i.e. not working)

### Questions

1. Why React.js ?

It seemed like a logical choice, keeping in mind the popularity of React nowadays and the vast amount of open source libs for it.
Also, I've found `react-chat-elements` to be pretty easy & clear to work with, it saved me a lot of time.
Using React, it's possible to KISS.

2. Why F# + Fable ?

Wanted to have strong typing & compiler holding my hand on the way, plus I have some experience with F# already.
I wanted to keep the business logic as pure as possible (i.e. free from side-effects) to make development easier and avoid bugs, 
and while I was considering using TypeScript my previous experience with functional code in TypeScript was quite poor.
   
3. Why is `X` feature is not supported ?

Either it was too hard to implement, or I didn't get to it yet. Please file an issue if you need it or have any questions about implementation.

4. Why 'esbuild' ?

It's fast.

5. Can we get a mobile (i.e. React Native) example ?

Possibly in the future, but not at the moment. I believe it's possible to migrate the app to mobile pretty easily.

6. Some question that is not listed here

Please file an issue, I'll try my best to reply.
