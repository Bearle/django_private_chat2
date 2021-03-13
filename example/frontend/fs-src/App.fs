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

let Identicon: obj = import "*" "identicon.js"

let getPhotoString (inputString: string) (size: int option) =
    let size = size |> Option.defaultValue 20
    let h = hash64 inputString
    let i = createNew (Identicon) (h, {|size=size;margin = 0|})
    "data:image/png;base64," + unbox<string> i

let generateRandomId(): int64 =
    let r = Random()
    -(r.Next()) |> int64

type MessageModel =
  {
   id: int
   text: string
   sent: DateTimeOffset
   edited: DateTimeOffset
   read: bool
   file: string option
   sender: string
   recipient: string
   sender_username: string
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
              sender = get.Required.Field "sender" Decode.string
              recipient = get.Required.Field "recipient" Decode.string
              sender_username =  get.Required.Field "sender_username" Decode.string
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
type SelfInfoResponse =
    { pk: string
      username: string
      }
    static member Decoder : Decoder<SelfInfoResponse> =
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

// TODO: make it "of string * int"  etc. with special field indicating tag
type ErrorTypes =
    | MessageParsingError = 1
    | TextMessageInvalid = 2
    | InvalidMessageReadId = 3
    | InvalidUserPk = 4

type ErrorDescription = ErrorTypes * string

type MessageTypeTextMessage =
    {
    random_id: int64
    text: string
    sender: string
    receiver: string
    sender_username: string
    }
    static member Decoder: Decoder<MessageTypeTextMessage> =
      Decode.object (fun get ->
          {
              random_id=get.Required.Field "random_id" Decode.int64
              text = get.Required.Field "text" Decode.string
              sender = get.Required.Field "sender" Decode.string
              receiver = get.Required.Field "receiver" Decode.string
              sender_username = get.Required.Field "sender_username" Decode.string
          })
type MessageTypeMessageIdCreated=
    {
    random_id: int64
    db_id: int64
    }
    static member Decoder: Decoder<MessageTypeMessageIdCreated> =
      Decode.object (fun get ->
          {
              random_id=get.Required.Field "random_id" Decode.int64
              db_id=get.Required.Field "db_id" Decode.int64
          })

type MessageTypeErrorOccured=
    {
    error: ErrorDescription
    }
    static member Decoder: Decoder<MessageTypeErrorOccured> =
      Decode.object (fun get ->
          {
              error=get.Required.Field "error" (Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string)
          })
type MessageTypes =
    | WentOnline = 1
    | WentOffline = 2
    | TextMessage = 3
    | FileMessage = 4
    | IsTyping = 5
    | MessageRead = 6
    | ErrorOccured = 7
    | MessageIdCreated = 8

let MessageTypesDecoder: Decoder<MessageTypes> = Decode.object (fun get -> get.Required.Field "msg_type" Decode.Enum.int<MessageTypes>)

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

type MessageBoxData = {
    dialog_id: string
    message_id: int64
    out: bool
}
type MessageBox = {
    position: MessageBoxPosition
    ``type``: MessageBoxType
    text: string
    title: string
    status: MessageBoxStatus
    avatar: string
    date: DateTimeOffset
    data: MessageBoxData
} with member this.HasDbId() = this.data.message_id > 0L

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
    dialogList: ChatItem array
    selectedDialog: ChatItem
    socket: WebSocket
}
let msgTypeEncoder (t:MessageTypes) data =
    let d = ["msg_type", Encode.Enum.int t ] |> List.append data
    Encode.object d |> Encode.toString 0


let createMessageBoxFromMessageTypeTextMessage (message: MessageTypeTextMessage) =
    let avatar = getPhotoString message.sender (Some 150)
    {
        position=MessageBoxPosition.Left
        ``type``=MessageBoxType.Text
        text = message.text
        title=message.sender_username
        status=MessageBoxStatus.Waiting
        avatar=avatar
        date=(DateTimeOffset(JS.Constructors.Date.Create()))
        data = {dialog_id=message.sender;message_id=message.random_id;out=false}
    }

let createMessageBoxFromOutgoingTextMessage (text: string) (user_pk:string) (self_pk:string) (self_username: string) (random_id:int64)=
    let avatar = getPhotoString self_pk (Some 150)
    {
        position=MessageBoxPosition.Right
        ``type``=MessageBoxType.Text
        text = text
        title=self_username
        status=MessageBoxStatus.Waiting
        avatar=avatar
        date=(DateTimeOffset(JS.Constructors.Date.Create()))
        data = {dialog_id=user_pk;message_id=random_id;out=true}
    }

let handleIncomingWebsocketMessage (sock: WebSocket) (message: string)
                                   (addMessage: MessageBox -> unit) (replaceMessageId: int64 -> int64 -> unit) =
    let res =
        Decode.fromString MessageTypesDecoder message
        |> Result.bind (fun o ->
            match o with
            | MessageTypes.TextMessage ->
                printfn "Received MessageTypes.TextMessage - %s" message

                let decoded = Decode.fromString MessageTypeTextMessage.Decoder message
                              |> Result.map createMessageBoxFromMessageTypeTextMessage
                match decoded with
                | Result.Ok d -> addMessage d |> Result.Ok
                | Result.Error e -> Result.Error e
            | MessageTypes.MessageIdCreated ->
                printfn "Received MessageTypes.MessageIdCreated - %s" message
                let decoded = Decode.fromString MessageTypeMessageIdCreated.Decoder message
                match decoded with
                | Result.Ok d -> replaceMessageId d.random_id d.db_id |> Result.Ok
                | Result.Error e -> Result.Error e

            | MessageTypes.ErrorOccured ->
                printfn "Received MessageTypes.ErrorOccured - %s" message
                let decoded = Decode.fromString MessageTypeErrorOccured.Decoder message
                match decoded with
                | Result.Ok err ->
                    let msg = sprintf "Error: %A, message %s" (fst err.error) (snd err.error)
                    Result.Error msg
                | Result.Error e -> Result.Error e
            | x ->
                printfn "Received unhandled MessageType %A" x
                Result.Ok ()
            )
    match res with
    | Result.Ok _  -> ()
    | Result.Error e ->
        printfn "Error while processing message %s - error: %s" message e
        let data = [
         "error", Encode.tuple2 (Encode.Enum.int) (Encode.string) (ErrorTypes.MessageParsingError, (sprintf "msg_type decoding error - %s" e))
        ]
        sock.send (msgTypeEncoder MessageTypes.ErrorOccured data)


//let decodeError s  = Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string s ErrorDescription

let sendOutgoingTextMessage (sock: WebSocket) (text: string) (user_pk: string) (self_info: SelfInfoResponse option) =
    printfn "Sending text message: '%A', user_pk:'%A'" text user_pk
    let randomId = generateRandomId()
    let data = [
        "text", Encode.string text
        "user_pk", Encode.string user_pk
        "random_id", Encode.int (int32 randomId)
    ]
    sock.send (msgTypeEncoder MessageTypes.TextMessage data)
    self_info |> Option.map (fun x -> createMessageBoxFromOutgoingTextMessage text user_pk x.pk x.username randomId)

let sendIsTypingMessage (sock: WebSocket) =
    sock.send (msgTypeEncoder MessageTypes.IsTyping [])

let backendUrl = "http://127.0.0.1:8000"
let messagesEndpoint = sprintf "%s/messages/" backendUrl
let dialogsEndpoint = sprintf "%s/dialogs/" backendUrl
let selfEndpoint = sprintf "%s/self/" backendUrl

let fetchSelfInfo() =
    promise {
        let! resp = tryFetch selfEndpoint []
        match resp with
        | Result.Ok r ->
            let! text = r.text()
            let decoded = Decode.fromString SelfInfoResponse.Decoder text
            return decoded
        | Result.Error e -> return Result.Error e.Message
    }

let fetchMessages() =
    promise {
        let! resp = tryFetch messagesEndpoint []
        match resp with
        | Result.Ok r ->
            let! text = r.text()
            let decoded = Decode.fromString MessagesResponse.Decoder text
            return decoded
        | Result.Error e -> return Result.Error e.Message
    }
    |> Promise.mapResult (fun x ->
        x.data
        |> Array.map (fun message ->
            let t = match message.file with |None -> MessageBoxType.Text |Some _ -> MessageBoxType.File
            let status =
                match message.out, message.read with
                | _, true ->  MessageBoxStatus.Read
                | true, false -> MessageBoxStatus.Sent
                | false, false -> MessageBoxStatus.Received
            let avatar = getPhotoString message.sender (Some 150)
            let dialog_id = if message.out then message.recipient else message.sender

            {
                position=if message.out then MessageBoxPosition.Right else MessageBoxPosition.Left
                ``type``=t
                text = message.text
                title=message.sender_username
                status=status
                avatar=avatar
                date=message.sent
                data = {dialog_id=dialog_id;message_id=int64 message.id;out=message.out}
            })
        |> Array.sortBy (fun x -> x.date)
        )
let filterMessagesForDialog (d: ChatItem option) (messages: MessageBox [])=
    match d with
    | Some dialog -> messages |> Array.filter (fun m -> m.data.dialog_id = dialog.id)
    | None -> Array.empty


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
            {
                id = dialog.other_user_id
                avatar = getPhotoString dialog.other_user_id None
                avatarFlexible = true
                statusColor = "lightgreen"
                statusColorType = None
                alt = dialog.username
                title = dialog.username
                date = dialog.created
                subtitle = "subtitle"
                unread = dialog.unread_count
            })
    )
