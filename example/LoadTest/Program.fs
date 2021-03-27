open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open NBomber.Contracts
open NBomber.FSharp
open System.Net.Http
open NBomber.Plugins.Http.FSharp
open Serilog
open Thoth.Json.Net


type UserInfoResponse =
        { pk: string; username: string }
        static member Decoder : Decoder<UserInfoResponse> =
            Decode.object
                (fun get ->
                    { pk = get.Required.Field "pk" Decode.string
                      username = get.Required.Field "username" Decode.string
                    }
                )
type DialogModel =
      {
       id: int
       created: DateTimeOffset
       modified: DateTimeOffset
       other_user_id: string
       unread_count: int
       username: string
      }
      static member Decoder: Decoder<DialogModel> =
          Decode.object (fun get ->
              {
                  id = get.Required.Field "id" Decode.int
                  created = (get.Required.Field "created" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
                  modified = (get.Required.Field "modified" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
                  other_user_id = get.Required.Field "other_user_id" Decode.string
                  unread_count = get.Required.Field "unread_count" Decode.int
                  username = get.Required.Field "username" Decode.string
              })

    type DialogsResponse =
        { page: int
          pages: int
          data: DialogModel array
          }
        static member Decoder : Decoder<DialogsResponse> =
            Decode.object
                (fun get ->
                    { page = get.Required.Field "page" Decode.int
                      pages = get.Required.Field "pages" Decode.int
                      data = get.Required.Field "data" (Decode.array DialogModel.Decoder)
                    }
                )

[<EntryPoint>]
let main argv =

    use httpClient = new HttpClient()

    let backendUrl = "http://127.0.0.1:8000"
    let loginEndpoint = sprintf "%s/admin/login/" backendUrl
    let messagesEndpoint = sprintf "%s/messages/" backendUrl
    let dialogsEndpoint = sprintf "%s/dialogs/" backendUrl
    let selfEndpoint = sprintf "%s/self/" backendUrl


    let get_csrf_cookie_step = HttpStep.create("get_csrf", fun context ->
        Http.createRequest "GET" loginEndpoint
        |> Http.withHeader "Accept" "application/json"
        |> Http.withCheck(fun response ->
            task {
                let c = response.Headers
                let cookieValues = if c.Contains "Set-Cookie" then Some (c.GetValues("Set-Cookie")) else None
                let cookie = cookieValues |> Option.bind (fun x -> x |> Array.ofSeq |> Array.tryHead)
                printfn "%A" cookie
                let csrfToken = cookie |> Option.map (fun x -> x.[x.IndexOf('=')+1..x.IndexOf(';')-1])
                match csrfToken with
                | Some token -> return Response.Ok(token)
                | None -> return Response.Fail()
            })
    )

    let login_step = HttpStep.create("login", fun context ->

        let username,password = ("delneg","")
        let csrfToken = context.GetPreviousStepResponse<string>()
        let d = seq {("username", username);("password",password);("csrfmiddlewaretoken",csrfToken)} |> dict
        let content = new FormUrlEncodedContent(d)

        printfn "Logging with %A" d
        Http.createRequest "POST" loginEndpoint
        |> Http.withBody(content)
        |> Http.withHeader "Cookie" ("csrftoken=" + csrfToken)
        |> Http.withCheck(fun response ->
            task {
                let headers = response.Headers
                printfn "%A" headers
                return if response.IsSuccessStatusCode then Response.Ok()
                       else Response.Fail("status code: " + response.StatusCode.ToString())
            })

    )


    let fetch_self_step = HttpStep.create("fetch_self", fun context ->
        Http.createRequest "GET" selfEndpoint
        |> Http.withHeader "Accept" "application/json"
        |> Http.withCheck(fun response -> task {
                let! json = response.Content.ReadAsStringAsync()
                let decoded = Decode.fromString UserInfoResponse.Decoder json

                match decoded with
                | Result.Ok _ -> return Response.Ok()
                | Result.Error _ -> return Response.Fail()
            })
    )

    let fetch_dialogs_step = HttpStep.create("fetch_dialogs", fun context ->
        Http.createRequest "GET" dialogsEndpoint
        |> Http.withHeader "Accept" "application/json"
        |> Http.withCheck(fun response -> task {
                let! json = response.Content.ReadAsStringAsync()
                let decoded = Decode.fromString DialogsResponse.Decoder json
                match decoded with
                | Result.Ok dialogs -> return Response.Ok(dialogs.data |> Array.map (fun x -> x.other_user_id))
                | Result.Error _ -> return Response.Fail()
            })
    )



    Scenario.create "django_private_chat2_loadtest" [get_csrf_cookie_step;login_step]
//    |> Scenario.withWarmUpDuration(seconds 1)
    |> Scenario.withLoadSimulations [KeepConstant(1, seconds 1)]
    |> NBomberRunner.registerScenario
    |> NBomberRunner.withLoggerConfig(fun () ->
        LoggerConfiguration().MinimumLevel.Verbose()
    )
    |> NBomberRunner.run

    |> ignore

    0 // return an integer exit code
