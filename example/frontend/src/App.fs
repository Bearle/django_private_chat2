module App

open System
open Browser.Types
open Fable.Core
open Fable.React
open App.AppTypes
open Feliz

JsInterop.importSideEffects "react-chat-elements/dist/main.css"
JsInterop.importSideEffects "react-toastify/dist/ReactToastify.css"
JsInterop.importSideEffects "./App.css"

// [<ImportDefault("reconnecting-websocket")>]
// type ReconnectingWebsocket(url: string) = nativeOnly

type ReconnectingWebsocket =
    [<Emit("new $0($1)")>]
    abstract Create: string -> Browser.Types.WebSocket


[<ImportDefault("lodash.throttle")>]
let lodash_throttle(fn: (unit -> unit) * float) = jsNative


[<ImportDefault("reconnecting-websocket")>]
let RWebSocket : ReconnectingWebsocket = jsNative


// [<ImportMember("react-toastify")>]
// let toast(text: string, ?options: obj): int = jsNative
module private Elmish =
    open Elmish
    type State = {
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
        socket = RWebSocket.Create("ws://" + Browser.Dom.window.location.host + "/chat_ws")}, Cmd.none

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
        | PerformSendingMessage of text: string
        | PerformFileUpload of Browser.Types.FileList
        | SetShowNewChatPopup of show: bool
        | SelectDialog of dialog: ChatItem
        | LoadUsersData
        | DialogFetchingFailed of error: string
        | FileUploadError of error: string
        | FileUploadSuccess of uploadRes: Result<MessageModelFile,string>
        | SetMsgIdAsRead of msg_id: int64

    let update (msg: Msg) (state: State) =
        match msg with
        | DialogFetchingFailed err ->
            printfn $"Failed to fetch dialogs -  {err}"
            {state with UsersDataLoading = false}, Cmd.none
        | DialogsFetched usersResults ->
            match usersResults with
            | Ok users ->
                printfn $"Fetched users {users}"
                {state with AvailableUsers=users},Cmd.none
            | Error s -> state, Cmd.ofMsg (DialogFetchingFailed s)
        | LoadUsersData ->
            let cmd = Cmd.OfPromise.either
                          Logic.fetchUsersList // Promise
                          state.dialogList // Argument
                          Msg.DialogsFetched // Map Success
                          (fun x -> DialogFetchingFailed (x.ToString())) // Map Exception
            {state with UsersDataLoading = true}, cmd
        | SetShowNewChatPopup show ->
            {state with ShowNewChatPopup = show}, Cmd.none
        | PerformSendingMessage text ->
            let msgBox =
                state.selectedDialog
                |> Option.map (fun x -> x.id)
                |> Option.bind (fun pk -> Logic.sendOutgoingTextMessage state.socket text pk state.selfInfo)

            printfn $"sendOutgoingTextMessage result:{msgBox}"
            match msgBox with
                | Some msg -> state, Cmd.ofMsg (Msg.AddMessage msg)
                | None -> state, Cmd.none

        | SetMsgIdAsRead msg_id ->
            printfn $"Setting {msg_id} as read"
            let newMsgList =
                state.messageList
                |> Array.map (fun msg ->
                    if msg.data.message_id = msg_id then
                        {msg with status = MessageBoxStatus.Read}
                    else
                        msg
                    )
            {state with messageList = newMsgList}, Cmd.none

        | PerformFileUpload files ->
            //TODO: set 'file uploading' state to true, show some indication of file upload in progress
            printfn "File upload starting..."
            let cookie = App.Utils.getCookie() |> Option.defaultValue ""
            let cmd = Cmd.OfPromise.either
                            (Logic.uploadFile files)
                            cookie
                            Msg.FileUploadSuccess
                            (fun x -> Msg.FileUploadError (x.ToString()))
            state, cmd
        | FileUploadSuccess fileResult ->
            match fileResult with
            | Ok file ->
                printfn $"Uploaded file : {file}"
                let user_pk = state.selectedDialog |> Option.map (fun x -> x.id)

                let msgBox = user_pk
                             |> Option.bind (fun pk -> Logic.sendOutgoingFileMessage state.socket pk file state.selfInfo)
                printfn $"sendOutgoingFileMessage result:{msgBox}"
                match msgBox with
                | Some msg -> state, Cmd.ofMsg (Msg.AddMessage msg)
                | None -> state, Cmd.none

            | Error s -> state, Cmd.ofMsg (Msg.FileUploadError s)
        | FileUploadError err ->
            printfn $"File upload error - {err}"
            state, Cmd.none
        | other -> printfn $"Received unsupported msg {other}, ignoring";state,Cmd.none
        // init()

module private Funcs =
    open Elmish
    open Browser.Types
    let TYPING_TIMEOUT: float = 5000

    let getConnectionStateText (socketState: WebSocketState) =
        match socketState with
        | WebSocketState.CONNECTING -> "Connecting..."
        | WebSocketState.OPEN -> "Connected"
        | WebSocketState.CLOSING -> "Disconnecting..."
        | WebSocketState.CLOSED -> "Disconnected"
        | _ -> "Unknown"


    let isTyping (socket: WebSocket) :unit->unit=
        lodash_throttle((fun () -> Logic.sendIsTypingMessage socket),TYPING_TIMEOUT)


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
    let MessageInputField (model:State) (dispatch: Msg -> unit) (triggerFileRefClick: unit -> unit) =
        let inputRef = React.useInputRef()
        let isTyping = Funcs.isTyping(model.socket)
        let leftBtnIcon = {|
                        ``component`` = JSX.jsx "<FaPaperclip/>"
                        size = 24
                    |}
        JSX.jsx
            $"""
        import {{ Input, Button }} from "react-chat-elements"
        import {{ FaPaperclip }} from "react-icons/fa"

        <Input
            placeholder="Type here to send a message."
            defaultValue=""
            referance={inputRef}
            multiline={true}
            onKeyPress={fun (e: KeyboardEvent) ->
              if e.charCode <> 13 then
                JS.console.log("key pressed")
                isTyping()

              if e.shiftKey && e.charCode = 13 then
                true
              elif e.charCode = 13 then
                if model.socket.readyState = WebSocketState.OPEN then
                    inputRef.current |> Option.iter (fun x ->
                        dispatch (Msg.PerformSendingMessage x.value)
                        x.value <- ""
                    )

                    e.preventDefault()
                false
              else
                false
            }
            leftButtons={{
                    <Button
                    type="transparent"
                    color="black"
                    onClick={triggerFileRefClick}
                    icon={leftBtnIcon}
                />
            }}
            rightButtons={{
                <Button
                    text='Send'
                    disabled={model.socket.readyState <> WebSocketState.OPEN}
                    onClick={fun () ->
                        inputRef.current |> Option.iter (fun x ->
                            dispatch (Msg.PerformSendingMessage x.value)
                            x.value <- ""
                        )
                        }/>
            }}/>

        """

    [<JSX.Component>]
    let SideBarChatList (model: State) (dispatch: Msg -> unit) =
        let searchInputRef = React.useInputRef()
        let searchIcon = {|
                        ``component`` = JSX.jsx "<FaSearch/>"
                        size = 18
                        |}
        let clearIcon = {|
                        ``component`` = JSX.jsx "<FaTimesCircle/>"
                        size = 18
                        |}

        let sidebarTop = JSX.jsx $"""
            <span className='chat-list'>
                <Input
                    placeholder="Search..."
                    referance={searchInputRef}
                    onKeyPress={fun (e: KeyboardEvent) ->
                      if e.charCode <> 13 then
                        // TODO:
                        // this.localSearch();
                        false
                      elif e.charCode = 13 then
                        // TODO:
                        // this.localSearch();
                        printfn $"Search invoke with '{searchInputRef.current |> Option.map (fun x -> x.value)}'"
                        e.preventDefault()
                        false
                      else
                        false
                    }
                    rightButtons={{
                    <div>
                        <Button
                            type="transparent"
                            color="black"
                            onClick={fun _ ->
                                // TODO:
                                // this.localSearch();
                                printfn $"Search invoke with '{searchInputRef.current |> Option.map (fun x -> x.value)}'"
                            }
                            icon={searchIcon}
                        />
                        <Button
                            type="transparent"
                            color="black"
                            onClick={fun _ -> searchInputRef.current |> Option.iter (fun x -> x.value <- "")}
                            icon={clearIcon}
                        />
                    </div>
                    }}
                />
                <ChatList
                    onClick={fun (item, i, e) -> dispatch (Msg.SelectDialog item)}
                    dataSource={model.filteredDialogList |> Array.sortByDescending (fun x -> x.date)}
                />
            </span>
        """
        let sidebarBottom = JSX.jsx $"""
            <Button
                type="transparent"
                color="black"
                disabled={true}
                text={$"Connection state: {Funcs.getConnectionStateText model.socket.readyState}"}
            />
        """
        let sidebarData = {|
                            top= sidebarTop
                            bottom=sidebarBottom
                            |}
        JSX.jsx $"""
           import {{ SideBar, Input, Button }} from "react-chat-elements"
           import {{ FaSearch, FaTimesCircle }} from "react-icons/fa"
           <SideBar
                type='light'
                data = {sidebarData}
           />
        """

    [<JSX.Component>]
    let PopUpRightPanel (model: State) (dispatch: Msg -> unit) =
        JSX.jsx $"""
            import {{ ChatList }} from "react-chat-elements"
            <ChatList onClick={fun (item, i, e) ->
                dispatch (Msg.SetShowNewChatPopup false)
                dispatch (Msg.SelectDialog item)
            }
            dataSource={model.AvailableUsers}/>
        """

    [<JSX.Component>]
    let NavbarRightPanel (model: State) (dispatch: Msg -> unit) =
        let rightBtnIcon = {|
                        ``component`` = JSX.jsx "<FaEdit/>"
                        size = 24
                    |}
        let id = model.selectedDialog |> Option.map (fun d -> d.id)
        JSX.jsx $"""
            import {{ Navbar, ChatItem }} from "react-chat-elements"
            import {{ FaEdit }} from "react-icons/fa"
            <Navbar
            left={{
                <ChatItem
                id={id}
                avatar={model.selectedDialog |> Option.map (fun d -> d.avatar)}
                avatarFlexible={model.selectedDialog |> Option.map (fun d -> d.avatarFlexible)}
                statusColorType={model.selectedDialog |> Option.map (fun d -> d.statusColorType)}
                alt={model.selectedDialog |> Option.map (fun d -> d.alt)}
                title={model.selectedDialog |> Option.map (fun d -> d.title)}
                date={{null}}
                unread={0}
                statusColor={model.selectedDialog
                             |> Option.filter (fun x -> model.onlinePKs |> Array.contains x.id)
                             |> Option.map (fun _ -> "lightgreen")
                             |> Option.defaultValue ""
                             }
                subtitle={model.selectedDialog
                             |> Option.filter (fun x -> model.typingPKs |> Array.contains x.id)
                             |> Option.map (fun _ -> "typing...")
                             |> Option.defaultValue ""
                          }
                />
            }}
            right={{
                <Button
                    type='transparent'
                    color='black'
                    onClick={fun _ ->
                        dispatch (Msg.LoadUsersData)
                        dispatch (Msg.SetShowNewChatPopup true)
                    }
                    icon={rightBtnIcon}
                />
            }}
            />
        """

[<JSX.Component>]
let App () =
    let model, dispatch = React.useElmish (Elmish.init, Elmish.update)

    let fileInputRef = React.useRef<HTMLInputElement option> (None)

    let triggerFileRefClick () = fileInputRef.current |> Option.iter (fun x -> x.click())


    JSX.jsx
        $"""
    import {{ ToastContainer }} from "react-toastify"
    import {{ MessageList, Popup }} from "react-chat-elements"
    import {{ FaWindowClose }} from "react-icons/fa"

    <div className="container">
        <div className="chat-list">
            {Components.SideBarChatList model dispatch}
        </div>
        <div className="right-panel">
            <ToastContainer/>

            <Popup
                show={model.ShowNewChatPopup}
                header='New chat'
                headerButtons = {
                                    [|
                    {|
                      ``type`` = "transparent"
                      color = "black"
                      text = "close"
                      icon = {|
                                size = 18
                                ``component`` = JSX.jsx "<FaWindowClose/>"
                               |}
                      onClick = fun _ -> dispatch (Elmish.Msg.SetShowNewChatPopup false)
                      |}

                |]
                }
                renderContent = {fun () ->
                    if model.UsersDataLoading then
                        JSX.jsx "<div><p>Loading data...</p></div>"
                    else
                        if model.AvailableUsers.Length = 0 then
                            JSX.jsx "<div><p>No users available</p></div>"
                        else
                            Components.PopUpRightPanel model dispatch
                    }
            />
            {Components.NavbarRightPanel model dispatch}
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
            {Components.MessageInputField model dispatch triggerFileRefClick}
        </div>
    </div>
    """
