module App

open System
open Fable.Core
open Fable.React

[<JSX.Component>]
let App () =
//     let model, dispatch = React.useElmish (init, update, arg = 2)

//         import 'react-chat-elements/dist/main.css';
//         import 'react-toastify/dist/ReactToastify.css';
//         import './App.css';
    JSX.jsx
        $"""

    <div className="container mx-5 mt-5 is-max-desktop">
        <p className="title">My Todos</p>
    </div>
    """
