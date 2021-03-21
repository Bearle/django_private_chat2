module App

open System
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open Fetch
open Thoth.Json
open App.AppTypes
open App.Utils

let getSubtitleTextFromMessageModel(msg: MessageModel option) =
    msg
   |> Option.map (fun x -> if x.out then "You: " + x.text else x.text)
   |> Option.defaultValue ""

let getSubtitleTextFromMessageBox(msg: MessageBox option) =
    msg
   |> Option.map (fun x -> if x.data.out then "You: " + x.text else x.text)
   |> Option.defaultValue ""

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

let createNewDialogModelFromIncomingMessageBox (m: MessageBox) =
    {
    id = m.data.dialog_id
    avatar = getPhotoString m.data.dialog_id None
    avatarFlexible = true
    statusColor = "lightgreen"
    statusColorType = None
    alt = m.title
    title = m.title
    date = m.date
    subtitle = m.text
    unread = 1
    }

type WSHandlingCallbacks =
    {
        addMessage: MessageBox -> unit
        replaceMessageId: int64 -> int64 -> unit
        addPKToTyping: string -> unit
        changePKOnlineStatus: string -> bool -> unit
        setMessageIdAsRead: int64 -> unit
        newUnreadCount: string -> int -> unit
    }
let handleIncomingWebsocketMessage (sock: WebSocket) (message: string) (callbacks: WSHandlingCallbacks) =
    let res =
        Decode.fromString MessageTypesDecoder message
        |> Result.bind (fun o ->
            match o with
            | MessageTypes.TextMessage ->
                printfn "Received MessageTypes.TextMessage - %s" message
                Decode.fromString MessageTypeTextMessage.Decoder message
                |> Result.map createMessageBoxFromMessageTypeTextMessage
                |> Result.map (callbacks.addMessage)

            | MessageTypes.MessageIdCreated ->
                printfn "Received MessageTypes.MessageIdCreated - %s" message
                Decode.fromString MessageTypeMessageIdCreated.Decoder message
                |> Result.map (fun d -> callbacks.replaceMessageId d.random_id d.db_id)

            | MessageTypes.IsTyping ->
                printfn "Received MessageTypes.IsTyping - %s" message
                Decode.fromString GenericUserPKMessage.Decoder message
                |> Result.map (fun d -> callbacks.addPKToTyping d.user_pk)

            | MessageTypes.WentOnline ->
                printfn "Received MessageTypes.WentOnline - %s" message
                Decode.fromString GenericUserPKMessage.Decoder message
                |> Result.map (fun d -> callbacks.changePKOnlineStatus d.user_pk true)

            | MessageTypes.WentOffline ->
                printfn "Received MessageTypes.WentOffline - %s" message
                Decode.fromString GenericUserPKMessage.Decoder message
                |> Result.map (fun d -> callbacks.changePKOnlineStatus d.user_pk false)

            | MessageTypes.MessageRead ->
                printfn "Received MessageTypes.MessageRead - %s" message
                Decode.fromString MessageTypeMessageRead.Decoder message
                |> Result.map (fun d -> callbacks.setMessageIdAsRead d.message_id)

            | MessageTypes.NewUnreadCount ->
                printfn "Received MessageTypes.NewUnreadCount - %s" message
                Decode.fromString MessageTypeNewUnreadCount.Decoder message
                |> Result.map (fun d -> callbacks.newUnreadCount d.sender d.unread_count)
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
    | Result.Ok _  -> None
    | Result.Error e ->
        printfn "Error while processing message %s - error: %s" message e
        let data = [
         "error", Encode.tuple2 (Encode.Enum.int) (Encode.string) (ErrorTypes.MessageParsingError, (sprintf "msg_type decoding error - %s" e))
        ]
        sock.send (msgTypeEncoder MessageTypes.ErrorOccured data)
        Some (sprintf "Error occured - %s" e)


//let decodeError s  = Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string s ErrorDescription

let sendOutgoingTextMessage (sock: WebSocket) (text: string) (user_pk: string) (self_info: UserInfoResponse option) =
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

let sendMessageReadMessage (sock: WebSocket) (user_pk: string) (message_id: int64) =
    printfn "Sending 'read' message for message_id '%i', user_pk:'%A'" message_id user_pk
    let data = [
        "user_pk", Encode.string user_pk
        "message_id", Encode.int (int32 message_id)
    ]
    sock.send (msgTypeEncoder MessageTypes.MessageRead data)

let backendUrl = "http://127.0.0.1:8000"
let messagesEndpoint = sprintf "%s/messages/" backendUrl
let dialogsEndpoint = sprintf "%s/dialogs/" backendUrl
let selfEndpoint = sprintf "%s/self/" backendUrl
let usersEndpoint = sprintf "%s/users/" backendUrl


let fetchSelfInfo() =
    promise {
        let! resp = tryFetch selfEndpoint []
        match resp with
        | Result.Ok r ->
            let! text = r.text()
            let decoded = Decode.fromString UserInfoResponse.Decoder text
            return decoded
        | Result.Error e -> return Result.Error e.Message
    }

let fetchUsersList(existing: ChatItem array) =
    let existingPks = existing |> Array.map (fun x -> x.id)
    promise {
        let! resp = tryFetch usersEndpoint []
        match resp with
        | Result.Ok r ->
            let! text = r.text()
            let decoded = Decode.fromString (Decode.array UserInfoResponse.Decoder) text

            return decoded
        | Result.Error e -> return Result.Error e.Message
    }
    |> Promise.mapResult (fun x ->
    x
    |> Array.filter (fun dialog -> not (Array.contains dialog.pk existingPks))
    |> Array.map (fun dialog ->
        {
            id = dialog.pk
            avatar = getPhotoString dialog.pk None
            avatarFlexible = true
            statusColor = ""
            statusColorType = None
            alt = dialog.username
            title = dialog.username
            date = DateTimeOffset.Now
            subtitle = ""
            unread = 0
        }))

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

let markMessagesForDialogAsRead (sock:WebSocket) (d: ChatItem) (messages: MessageBox []) (msgReadCallback: int64 -> unit)=
    filterMessagesForDialog (Some d) messages
    |> Array.filter (fun y -> y.status <> MessageBoxStatus.Read && y.data.out = false && y.HasDbId() )
    |> Array.iter (fun x ->
        do msgReadCallback x.data.message_id
        do sendMessageReadMessage sock d.id x.data.message_id
    )


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
                statusColor = ""
                statusColorType = None
                alt = dialog.username
                title = dialog.username
                date = dialog.last_message |> Option.map (fun x -> x.sent) |> Option.defaultValue dialog.created
                subtitle = getSubtitleTextFromMessageModel dialog.last_message
                unread = dialog.unread_count
            })
    )
