module App

open System
open Browser.Types
open Fable.Core
open Fable.React
open App.AppTypes


[<ImportDefault("reconnecting-websocket")>]
let ReconnectingWebsocket(url: string): Browser.Types.WebSocket = nativeOnly

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



module private Components =
    open Elmish
    open Feliz
    open Browser.Types


    [<JSX.Component>]
    let Button (ttype: string) (color: string) onClick =
        JSX.jsx
            $"""
        <Button
            type='transparent'
            color='black'
            onClick={onClick}
            icon={{
                component: <FaPaperclip/>,
                size: 24
            }}
        />
        """

    [<JSX.Component>]
    let InputField (model:EState) (dispatch: Msg -> unit) =
        let inputRef = React.useRef<HTMLInputElement option> (None)

        JSX.jsx
            $"""
        import {{ Input, Button }} from "react-chat-elements"
        import {{ FaPaperclip}} from "react-icons/fa"

        <Input
            placeholder="Type here to send a message."
            defaultValue=""
            ref={inputRef}
            multiline={true}
            // buttonsFloat='left'
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
            leftButtons={{
                <Button
                    type='transparent'
                    color='black'
                />
            }}
            rightButtons={{
                <Button
                    text='Send'
                    disabled={model.socket.readyState <> WebSocketState.OPEN}
                    onClick={fun () -> dispatch Msg.PerformSendingMessage}/>
            }}/>

        """


[<JSX.Component>]
let App () =
//     let model, dispatch = React.useElmish (init, update, arg = 2)

//         import 'react-chat-elements/dist/main.css';
//         import 'react-toastify/dist/ReactToastify.css';
//         import './App.css';
    JSX.jsx
        $"""
    import {{ ToastContainer }} from "react-toastify"

    <div className="container">
        <div className="chat-list">
        </div>
        <div className="right-panel">
            <ToastContainer/>
        </div>
    </div>
    """
