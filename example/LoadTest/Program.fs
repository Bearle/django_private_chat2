open System
open System.Net.WebSockets
open FSharp.Control.Tasks.V2.ContextInsensitive
open NBomber.Contracts
open NBomber.FSharp
open System.Net.Http
open NBomber.Plugins.Http.FSharp
open Serilog
open Thoth.Json.Net
open Websocket.Client


let f = Bogus.Faker()
let rnd = Random()
type UserInfoResponse =
        { pk: string; username: string }
        static member Decoder : Decoder<UserInfoResponse> =
            Decode.object
                (fun get ->
                    { pk = get.Required.Field "pk" Decode.string
                      username = get.Required.Field "username" Decode.string
                    }
                )
type MessageTypes =
        | WentOnline = 1
        | WentOffline = 2
        | TextMessage = 3
        | FileMessage = 4
        | IsTyping = 5
        | MessageRead = 6
        | ErrorOccured = 7
        | MessageIdCreated = 8
        | NewUnreadCount = 9

let msgTypeEncoder (t:MessageTypes) data =
    let d = ["msg_type", Encode.Enum.int t ] |> List.append data
    Encode.object d |> Encode.toString 0

let generateRandomId(): int64 = -(rnd.Next()) |> int64

let sendOutgoingTextMessage (client: WebsocketClient) (text: string) (user_pk: string) =
    let randomId = generateRandomId()
    let data = [
        "text", Encode.string text
        "user_pk", Encode.string user_pk
        "random_id", Encode.int (int32 randomId)
    ]
    client.Send (msgTypeEncoder MessageTypes.TextMessage data)

let sendIsTypingMessage (sock: WebsocketClient) =
    sock.Send (msgTypeEncoder MessageTypes.IsTyping [])


[<EntryPoint>]
let main argv =

    use httpClient = new HttpClient()

    let host = "127.0.0.1:8000"
    let backendUrl = $"http://{host}"
    let wsEndpoint = $"ws://{host}/chat_ws"
    let loginEndpoint = sprintf "%s/dumb_auth/" backendUrl
    let messagesEndpoint user_pk = sprintf "%s/messages/%s" backendUrl user_pk
    let usersEndpoint = sprintf "%s/users/" backendUrl
    let selfEndpoint = sprintf "%s/self/" backendUrl

    let getCookieValue (s:string) =
        s.[s.IndexOf('=')+1..s.IndexOf(';')-1]

    let login_step = HttpStep.create("login", fun context ->

        let username,password = (f.Internet.UserName(),f.Internet.Password())
        let d = seq {("username", username);("password",password)} |> dict
        let content = new FormUrlEncodedContent(d)


        Http.createRequest "POST" loginEndpoint
        |> Http.withBody(content)
        |> Http.withCheck(fun response ->
            task {
                let cookieValues = if response.Headers.Contains "Set-Cookie" then Some (response.Headers.GetValues("Set-Cookie")) else None
                let cookie = cookieValues |> Option.map (fun x -> x |> Array.ofSeq |> Array.map getCookieValue)
                let csrfToken = cookie |> Option.bind (Array.tryItem 0)
                let sessionId = cookie |> Option.bind (Array.tryItem 1)

                match csrfToken, sessionId with
                | Some token, Some i ->
                    context.Logger.Information($"Logged in with {username}")
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
                    context.Data.["selfPk"] <- d.pk
                    context.Data.["selfUsername"] <- d.username
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

    let choose_random_user_and_get_messages_step = HttpStep.create("choose_random_user_and_get_messages", fun context ->
        let userId = (context.Data.["dialogIds"] |> string).Split(',')
                     |> (fun x -> Array.item (rnd.Next(x.Length)) x)


        Http.createRequest "GET" (messagesEndpoint userId)
        |> Http.withHeader "Accept" "application/json"
        |> Http.withHeader "Cookie" (string context.Data.["cookie"])
        |> Http.withCheck(fun response -> task {
                match response.IsSuccessStatusCode with
                | true  ->
                    context.Data.["userId"] <- userId

                    return Response.Ok()
                | false -> return Response.Fail()
            })
    )

    let connect_step = Step.create("ws_connect", fun context -> task {
        let url = Uri(wsEndpoint)
        let ws_factory = Func<ClientWebSocket>(fun _ ->
            let client = new ClientWebSocket()
            client.Options.SetRequestHeader("Cookie",(string context.Data.["cookie"]))
            client
        )
//        let exitEvent = new Threading.ManualResetEvent(false);
        let client = new WebsocketClient(url,ws_factory)
        client.Name <- (string context.Data.["selfUsername"])
        client.ReconnectTimeout <- seconds 10
        use _ = client.ReconnectionHappened
                |> Observable.subscribe (fun x -> context.Logger.Information($"[{client.Name}] Reconnection happened of type {x.Type}"))
        use _ = client.MessageReceived
                |> Observable.subscribe (fun x -> context.Logger.Information($"[{client.Name}] Message received - {x}"))

        do! client.Start()
        context.Logger.Information($"[{client.Name}] Started.")
        context.Data.["wsClient"] <- client

        return Response.Ok()
    })


    let send_typing_step = Step.create("ws_typing", fun context -> task {
         let client = context.Data.["wsClient"] :?> WebsocketClient
         context.Logger.Information($"[{client.Name}] Sending typing message...")
         Threading.Tasks.Task.Run(fun _ -> sendIsTypingMessage client) |> ignore
         if (not client.IsRunning) then
            return Response.Fail()
         else
            return Response.Ok()
    })

    let send_message_step = Step.create("ws_send_message", fun context -> task {
        let client = context.Data.["wsClient"] :?> WebsocketClient
        let userId = context.Data.["userId"] |> string
        let text = f.Lorem.Sentence()
        context.Logger.Information($"[{client.Name}] Sending text message to {userId}")
        Threading.Tasks.Task.Run(fun _ -> sendOutgoingTextMessage client text userId) |> ignore
        if (not client.IsRunning) then
            return Response.Fail()
         else
            return Response.Ok()
    })




    let typingPause = Step.createPause(fun () -> rnd.Next(500,2000) |> milliseconds)
    let beforeNextMessagePause = Step.createPause(fun () -> rnd.Next(2000,5000) |> milliseconds)

    let loopSteps = [
        for _ in 1..100 do
            yield send_typing_step
            yield typingPause
            yield send_message_step
            yield beforeNextMessagePause
    ]


    let steps = [
        login_step
        fetch_self_step
        connect_step
        fetch_users_step
        choose_random_user_and_get_messages_step

    ]
    let allSteps = List.append steps loopSteps

    Scenario.create "django_private_chat2_loadtest" allSteps
//    |> Scenario.withWarmUpDuration(seconds 1)
//    |> Scenario.withLoadSimulations [KeepConstant(10, seconds 1)]
    |> Scenario.withLoadSimulations [
        InjectPerSec(rate = 100, during = seconds 10)
    ]
    |> NBomberRunner.registerScenario
    |> NBomberRunner.withLoggerConfig(fun () ->
        LoggerConfiguration().MinimumLevel.Information()
    )
    |> NBomberRunner.run

    |> ignore

    0 // return an integer exit code
