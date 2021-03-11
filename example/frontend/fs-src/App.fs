module App

open System
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open Fetch
open Thoth.Json
type MessageModel =
  {
   id: int
   text: string
   sent: DateTimeOffset
   edited: DateTimeOffset
   read: bool
   file: string option
   sender: int
   recipient: int }
  static member Decoder: Decoder<MessageModel> =
      Decode.object (fun get ->
          {
              id = get.Required.Field "id" Decode.int
              text = get.Required.Field "text" Decode.string
              sent = (get.Required.Field "sent" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
              edited = (get.Required.Field "edited" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
              read = get.Required.Field "read" Decode.bool
              file = get.Optional.Field "file" Decode.string
              sender = get.Required.Field "sender" Decode.int
              recipient = get.Required.Field "recipient" Decode.int
          })
type MessagesResponse =
    { page: int
      pages: int
      data: MessageModel array
      }
    static member Decoder : Decoder<MessagesResponse> =
        Decode.object
            (fun get ->
                { page = get.Required.Field "page" Decode.int
                  pages = get.Required.Field "pages" Decode.int
                  data = get.Required.Field "data" (Decode.array MessageModel.Decoder)
                }
            )

// TODO: make it "of string * int"  etc. with special field indicating tag
type ErrorTypes =
    | MessageParsingError = 1
    | TextMessageBlank = 2
    | TextMessageTooLong = 3
    | InvalidMessageReadId = 4

type ErrorDescription = ErrorTypes * string

type MessageTypes =
    | WentOnline = 1
    | WentOffline = 2
    | TextMessage = 3
    | FileMessage = 4
    | IsTyping = 5
    | MessageRead = 6
    | ErrorOccured = 7

let msgTypeEncoder (t:MessageTypes) data =
    let d = ["msg_type", Encode.Enum.int t ] |> List.append data
    Encode.object d |> Encode.toString 0


//let decodeError s  = Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string s ErrorDescription

let sendIsTypingMessage (sock: WebSocket) =
    sock.send (msgTypeEncoder MessageTypes.IsTyping [])

let backendUrl = "http://127.0.0.1:8000"
let messagesEndpoint = sprintf "%s/messages/" backendUrl
let fetchMessages() = promise {
    let! resp = tryFetch messagesEndpoint []
    match resp with
    | Result.Ok r ->
        let! text = r.text()
        let decoded = Decode.fromString MessagesResponse.Decoder text
        return decoded
    | Result.Error e -> return Result.Error e.Message
}

let sayHelloFable() = "Hello Fable!"
