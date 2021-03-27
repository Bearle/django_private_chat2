open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open NBomber.Contracts
open NBomber.FSharp
open System.Net.Http
open NBomber.Plugins.Http.FSharp
open Serilog
open Thoth.Json.Net

let f = Bogus.Faker();

type UserInfoResponse =
        { pk: string; username: string }
        static member Decoder : Decoder<UserInfoResponse> =
            Decode.object
                (fun get ->
                    { pk = get.Required.Field "pk" Decode.string
                      username = get.Required.Field "username" Decode.string
                    }
                )

[<EntryPoint>]
let main argv =

    use httpClient = new HttpClient()

    let backendUrl = "http://127.0.0.1:8000"
    let loginEndpoint = sprintf "%s/dumb_auth/" backendUrl
    let messagesEndpoint = sprintf "%s/messages/" backendUrl
    let usersEndpoint = sprintf "%s/users/" backendUrl
    let selfEndpoint = sprintf "%s/self/" backendUrl

    let getCookieValue (s:string) =
        s.[s.IndexOf('=')+1..s.IndexOf(';')-1]

    let login_step = HttpStep.create("login", fun context ->

        let username,password = (f.Internet.UserName(),f.Internet.Password())
        let d = seq {("username", username);("password",password)} |> dict
        let content = new FormUrlEncodedContent(d)

        printfn "Logging with %A" d
        Http.createRequest "POST" loginEndpoint
        |> Http.withBody(content)
        |> Http.withCheck(fun response ->
            printfn "%A" response
            task {
                let cookieValues = if response.Headers.Contains "Set-Cookie" then Some (response.Headers.GetValues("Set-Cookie")) else None
                let cookie = cookieValues |> Option.map (fun x -> x |> Array.ofSeq |> Array.map getCookieValue)
                let csrfToken = cookie |> Option.bind (Array.tryItem 0)
                let sessionId = cookie |> Option.bind (Array.tryItem 1)

                match csrfToken, sessionId with
                | Some token, Some i ->
                    context.Data.["cookie"] <- $"csrftoken={token}; sessionid={i}"
                    return Response.Ok ()
                | _ -> return Response.Fail("status code: " + response.StatusCode.ToString())
            })
    )


    let fetch_self_step = HttpStep.create("fetch_self", fun context ->
        Http.createRequest "GET" selfEndpoint
        |> Http.withHeader "Cookie" (string context.Data.["cookie"])
        |> Http.withCheck(fun response -> task {
                let! json = response.Content.ReadAsStringAsync()
                let decoded = Decode.fromString UserInfoResponse.Decoder json

                match decoded with
                | Result.Ok d ->
                    context.Data.["selfInfo"] <- $"{d.pk},{d.username}"
                    return Response.Ok()
                | Result.Error _ -> return Response.Fail()
            })
    )

    let fetch_users_step = HttpStep.create("fetch_dialogs", fun context ->
        Http.createRequest "GET" usersEndpoint
        |> Http.withHeader "Accept" "application/json"
        |> Http.withHeader "Cookie" (string context.Data.["cookie"])
        |> Http.withCheck(fun response -> task {
                let! json = response.Content.ReadAsStringAsync()
                let decoded = Decode.fromString (Decode.array UserInfoResponse.Decoder) json
                match decoded with
                | Result.Ok dialogs ->
                    let dialogIds = dialogs|> Array.map (fun x -> x.pk) |> String.concat ","
                    context.Data.["dialogIds"] <- dialogIds
                    return Response.Ok()
                | Result.Error _ -> return Response.Fail()
            })
    )



    Scenario.create "django_private_chat2_loadtest" [login_step;fetch_self_step;fetch_users_step]
//    |> Scenario.withWarmUpDuration(seconds 1)
    |> Scenario.withLoadSimulations [KeepConstant(1, seconds 1)]
    |> NBomberRunner.registerScenario
    |> NBomberRunner.withLoggerConfig(fun () ->
        LoggerConfiguration().MinimumLevel.Verbose()
    )
    |> NBomberRunner.run

    |> ignore

    0 // return an integer exit code
