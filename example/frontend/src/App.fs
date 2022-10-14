module App

open System
open Browser.Types
open Fable.Core
open Fable.React
open App.AppTypes
open Feliz


[<ImportDefault("reconnecting-websocket")>]
let ReconnectingWebsocket(url: string): Browser.Types.WebSocket = nativeOnly

[<ImportMember("react-icons/fa")>]
let FaPaperclip: obj = jsNative

// [<ImportMember("react-toastify")>]
// let toast(text: string, ?options: obj): int = jsNative
module private Elmish =
    open Elmish
    type EState = {
        SocketConnectionState: int
        ShowNewChatPopup: bool
        UsersDataLoading: bool
        AvailableUsers: ChatItem[]
        messageList: MessageBox[]
        dialogList: ChatItem[]
        filteredDialogList: ChatItem[]
        typingPKs: string[]
        onlinePKs: string[]
        selfInfo: UserInfoResponse option
        selectedDialog: ChatItem option
        socket: Browser.Types.WebSocket
    }

    let init () = {
        SocketConnectionState = 0
        ShowNewChatPopup = false
        UsersDataLoading = false
        AvailableUsers = Array.empty
        messageList = Array.empty
        dialogList = Array.empty
        filteredDialogList = Array.empty
        typingPKs = Array.empty
        onlinePKs = Array.empty
        selfInfo = None
        selectedDialog = None
        socket = ReconnectingWebsocket("ws://" + Browser.Dom.window.location.host + "/chat_ws")}, Cmd.none

    type Msg =
        | SocketConnectionStateChanged of int
        | MessagesFetched of messages: Result<MessageBox[],string>
        | DialogsFetched of dialogs: Result<ChatItem[],string>
        | SelfInfoFetched of selfInfo: Result<UserInfoResponse,string>
        | DialogsFiltered of dialogsFiltered: ChatItem[]
        | AddTyping of pk: string
        | ChangeOnline of pk: string
        | AddMessage of msg: MessageBox
        | MessageIdChanged of old_int: string * new_id: string
        | UnreadCountChanged of id: string * count: int
        | SetMessageIdRead of id: string
        | PerformSendingMessage
        | PerformFileUpload of Browser.Types.FileList

    let update (msg: Msg) (state: EState) =
        init()


module private Components =
    open Elmish
    open Browser.Types


    [<JSX.Component>]
    let Button (ttype: string) (color: string) (iconComponent: obj) (size: int) (disabled: bool) onClick =
        let icon = {|
            ``component`` = iconComponent
            size = size
        |}
        JSX.jsx
            $"""
        <Button
            type="{ttype}"
            color="{color}"
            onClick={onClick}
            icon={icon}
            disabled={disabled}
        />
        """

    [<JSX.Component>]
    let MessageInputField (model:EState) (dispatch: Msg -> unit) =
        let inputRef = React.useRef<HTMLInputElement option> (None)

        // import {{ FaPaperclip}} from "react-icons/fa"

        JSX.jsx
            $"""
        import {{ Input, Button }} from "react-chat-elements"

        <Input
            placeholder="Type here to send a message."
            defaultValue=""
            ref={inputRef}
            multiline={true}
            onKeyPress={fun (e: KeyboardEvent) ->
              if e.charCode <> 13 then
                JS.console.log("key pressed")
                // TODO:
                // this.isTyping()
              if e.shiftKey && e.charCode = 13 then
                true
              elif e.charCode = 13 then
                if model.socket.readyState = WebSocketState.OPEN then
                    dispatch Msg.PerformSendingMessage
                    e.preventDefault()
                false
              else
                false
            }
            leftButtons={Button "transparent" "black" FaPaperclip 24 false (fun _ -> ())}
            rightButtons={{
                <Button
                    text='Send'
                    disabled={model.socket.readyState <> WebSocketState.OPEN}
                    onClick={fun () -> dispatch Msg.PerformSendingMessage}/>
            }}/>

        """


[<JSX.Component>]
let App () =
    let model, dispatch = React.useElmish (Elmish.init, Elmish.update)

//         import 'react-chat-elements/dist/main.css';
//         import 'react-toastify/dist/ReactToastify.css';
//         import './App.css';
    let fileInputRef = React.useRef<HTMLInputElement option> (None)
    JSX.jsx
        $"""
    import {{ ToastContainer }} from "react-toastify"
    import {{ MessageList }} from "react-chat-elements"

    <div className="container">
        <div className="chat-list">
        </div>
        <div className="right-panel">
            <ToastContainer/>

            <MessageList
                className='message-list'
                lockable={true}
                downButtonBadge={model.selectedDialog
                                    |> Option.map (fun d -> d.unread)
                                    |> Option.filter (fun x -> x > 0)
                                    |> Option.map string
                                    |> Option.defaultValue ""}
                dataSource={Logic.filterMessagesForDialog model.selectedDialog model.messageList}/>

            <input id='selectFile'
                hidden
                type="file"
                onChange={fun (e: Browser.Types.Event) ->
                    let files = (e.target :?> HTMLInputElement).files
                    dispatch (Elmish.Msg.PerformFileUpload files)}
                ref={fileInputRef}
                />
            {Components.MessageInputField model dispatch}
        </div>
    </div>
    """
