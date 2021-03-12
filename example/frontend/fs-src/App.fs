module App

open System
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open Fetch
open Thoth.Json

// https://stackoverflow.com/a/22429679

[<Emit("hval ^= str.charCodeAt(i)")>]
let assignBitShiftChartCode() = jsNative
[<Emit("('0000000' + ($0 >>> 0).toString(16)).substr(-8)")>]
let convert_to_hex(s: int): string = jsNative
let hashFnv32a(str: string, asString: bool, seed: int32 option) =
    let mutable hval: int32 = if seed.IsNone then 0x811c9dc5 else seed.Value
    for i = 0 to str.Length do
        assignBitShiftChartCode()
        hval <- hval + (hval <<< 1) + (hval <<< 4) + (hval <<< 7) + (hval <<< 8) + (hval <<< 24)
    if asString then
        convert_to_hex(hval)
    else
        (hval >>> 0).ToString()

let hash64 (str: string) =
    let mutable h1 = hashFnv32a(str, true, None)
    h1 + hashFnv32a(h1 + str, true, None)

let inline getDULowercaseName d = (string (d)).ToLower()

let Identicon: obj = import "*" "identicon.js"

let getPhotoString (inputString: string) (size: int option) =
    let size = size |> Option.defaultValue 20
    let h = hash64 inputString
    let i = createNew (Identicon) (h, {|size=size;margin = 0|})
    "data:image/png;base64," + unbox<string> i

type MessageModel =
  {
   id: int
   text: string
   sent: DateTimeOffset
   edited: DateTimeOffset
   read: bool
   file: string option
   sender: int
   recipient: int
   out: bool }
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
              out = get.Required.Field "out" Decode.bool
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

type DialogModel =
  {
   id: int
   created: DateTimeOffset
   modified: DateTimeOffset
   other_user_id: int
   unread_count: int
  }
  static member Decoder: Decoder<DialogModel> =
      Decode.object (fun get ->
          {
              id = get.Required.Field "id" Decode.int
              created = (get.Required.Field "created" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
              modified = (get.Required.Field "modified" Decode.int64) |> DateTimeOffset.FromUnixTimeSeconds
              other_user_id = get.Required.Field "other_user_id" Decode.int
              unread_count = get.Required.Field "unread_count" Decode.int
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

[<StringEnum>]
type MessageBoxPosition =
    | Left
    | Right

[<StringEnum>]
type MessageBoxStatus =
    | Waiting
    | Sent
    | Received
    | Read

[<StringEnum>]
type MessageBoxType =
    | Text
    | Location
    | Photo
    | Video
    | File
    | Spotify
    | Meeting
    | Audio

type MessageBox = {
    position: MessageBoxPosition
    ``type``: MessageBoxType
    text: string
    title: string
    status: MessageBoxStatus
    avatar: string
}

type ChatItem = {
    id: string
    avatar: string
    avatarFlexible: bool
    statusColor: string
    statusColorType: string option
    alt: string
    title: string
    date: DateTimeOffset
    subtitle: string
    unread: int
}
type State = {
    socketConnectionState: int
    messageList: MessageBox array
    socket: WebSocket
}
let msgTypeEncoder (t:MessageTypes) data =
    let d = ["msg_type", Encode.Enum.int t ] |> List.append data
    Encode.object d |> Encode.toString 0


//let decodeError s  = Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string s ErrorDescription

let sendIsTypingMessage (sock: WebSocket) =
    sock.send (msgTypeEncoder MessageTypes.IsTyping [])

let backendUrl = "http://127.0.0.1:8000"
let messagesEndpoint = sprintf "%s/messages/" backendUrl
let dialogsEndpoint = sprintf "%s/dialogs/" backendUrl

let fetchMessages() = promise {
    let! resp = tryFetch messagesEndpoint []
    match resp with
    | Result.Ok r ->
        let! text = r.text()
        let decoded = Decode.fromString MessagesResponse.Decoder text
        return decoded
    | Result.Error e -> return Result.Error e.Message
}

let fetchDialogs() =
    promise {
        let! resp = tryFetch dialogsEndpoint []

        match resp with
        | Result.Ok r ->
            let! text = r.text()
            let decoded = Decode.fromString DialogsResponse.Decoder text
            return decoded
        | Result.Error e -> return Result.Error e.Message
    }
    |> Promise.mapResult (fun x ->
        x.data
        |> Array.map (fun dialog ->
            let stringId = string dialog.other_user_id
            {
                id = stringId
                avatar = getPhotoString stringId None
                avatarFlexible = true
                statusColor = "lightgreen"
                statusColorType = None
                alt = "alt"
                title = "title"
                date = dialog.created
                subtitle = "subtitle"
                unread = dialog.unread_count
            })
    )
