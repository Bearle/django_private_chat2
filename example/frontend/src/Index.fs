module Index

open Fable.Core
open Browser
open Fable.React
open App.Utils
// Entry point must be in a separate file
// for Vite Hot Reload to work

[<JSX.Component>]
let MainApp () = App.App()

let root = ReactDomClient.createRoot (document.getElementById ("root"))
root.render (MainApp() |> toReact)
