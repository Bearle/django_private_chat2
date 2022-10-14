namespace App
module AppTypes =
    open System
    open Browser.Types
    open Fable.Core
    open Thoth.Json


    type MessageModelFile =
        { id: string
          url: string
          name: string
          size: int
          }
        static member Decoder : Decoder<MessageModelFile> =
            Decode.object
                (fun get ->
                    {
                      id = get.Required.Field "id" Decode.string
                      url = get.Required.Field "url" Decode.string
                      name = get.Required.Field "name" Decode.string
                      size = get.Required.Field "size" Decode.int
                    }
                )
    type MessageModel =
      {
       id: int
       text: string
       sent: DateTimeOffset
       edited: DateTimeOffset
       read: bool
       file: MessageModelFile option
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
                  file = get.Optional.Field "file" MessageModelFile.Decoder
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
       last_message: MessageModel option
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
                  last_message = get.Required.Field "last_message" (Decode.option MessageModel.Decoder)
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

    type MessageTypeNewUnreadCount =
        {
        sender: string
        unread_count: int
        }
        static member Decoder: Decoder<MessageTypeNewUnreadCount> =
          Decode.object (fun get ->
              {
                  sender = get.Required.Field "sender" Decode.string
                  unread_count = get.Required.Field "unread_count" Decode.int
              })

    type MessageTypeMessageRead =
        {
        message_id: int64
        sender: string
        receiver: string
        }
        static member Decoder: Decoder<MessageTypeMessageRead> =
          Decode.object (fun get ->
              {
                  message_id=get.Required.Field "message_id" Decode.int64
                  sender = get.Required.Field "sender" Decode.string
                  receiver = get.Required.Field "receiver" Decode.string
              })
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

    type MessageTypeFileMessage =
        {
        db_id: int64
        file: MessageModelFile
        sender: string
        receiver: string
        sender_username: string
        }
        static member Decoder: Decoder<MessageTypeFileMessage> =
          Decode.object (fun get ->
              {
                  db_id = get.Required.Field "db_id" Decode.int64
                  file = get.Required.Field "file" MessageModelFile.Decoder
                  sender = get.Required.Field "sender" Decode.string
                  receiver = get.Required.Field "receiver" Decode.string
                  sender_username = get.Required.Field "sender_username" Decode.string
              })

    type MessageTypeMessageIdCreated =
        { random_id: int64
          db_id: int64
        }
        static member Decoder: Decoder<MessageTypeMessageIdCreated> =
          Decode.object (fun get ->
              {
                  random_id=get.Required.Field "random_id" Decode.int64
                  db_id=get.Required.Field "db_id" Decode.int64
              })

    type ErrorTypes =
        | MessageParsingError = 1
        | TextMessageInvalid = 2
        | InvalidMessageReadId = 3
        | InvalidUserPk = 4
        | InvalidRandomId = 5
        | FileMessageInvalid = 6
        | FileDoesNotExist = 7

    type ErrorDescription = ErrorTypes * string

    type MessageTypeErrorOccurred =
        { error: ErrorDescription }
        static member Decoder: Decoder<MessageTypeErrorOccurred> =
          Decode.object (fun get ->
              { error=get.Required.Field "error" (Decode.tuple2 Decode.Enum.int<ErrorTypes> Decode.string) })

    type GenericUserPKMessage =
        { user_pk: string }
        static member Decoder: Decoder<GenericUserPKMessage> =
          Decode.object (fun get -> { user_pk=get.Required.Field "user_pk" Decode.string })

    type MessageTypes =
        | WentOnline = 1
        | WentOffline = 2
        | TextMessage = 3
        | FileMessage = 4
        | IsTyping = 5
        | MessageRead = 6
        | ErrorOccurred = 7
        | MessageIdCreated = 8
        | NewUnreadCount = 9

    let msgTypeEncoder (t:MessageTypes) data =
        let d = ["msg_type", Encode.Enum.int t ] |> List.append data
        Encode.object d |> Encode.toString 0

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

    type MessageBoxDataStatus = {
        click: bool
        loading: float
        download: bool
    }
    type MessageBoxData = {
        dialog_id: string
        message_id: int64
        out: bool
        size: string option
        uri: string option
        status: MessageBoxDataStatus option
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
        onDownload: (obj -> unit) option
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
