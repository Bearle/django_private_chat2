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

type ReconnectingWebsocket =
    [<Emit("new $0($1)")>]
    abstract Create: string -> Browser.Types.WebSocket


[<ImportDefault("lodash.throttle")>]
let lodash_throttle(fn: (unit -> unit) * float) = jsNative


[<ImportDefault("reconnecting-websocket")>]
let RWebSocket : ReconnectingWebsocket = jsNative


let toastOptions: App.Utils.ToastOptions = {
    autoClose = 1500
    hideProgressBar = true
    closeOnClick = false
    pauseOnHover = false
    pauseOnFocusLoss = false
    draggable = false
}

let TYPING_TIMEOUT: int = 5000

// [<ImportMember("react-toastify")>]
// let toast(text: string, ?options: obj): int = jsNative
module private Elmish =
    open Elmish
    open App.Utils
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



    type Msg =
        | SocketConnectionStateChanged of WebSocketState
        | LoadMessages
        | MessagesFetched of messages: Result<MessageBox[],string>
        | MessagesFetchingFailed of error: string
        | LoadUsersData
        | UsersDataFetched of users: Result<ChatItem[],string>
        | UsersDataFetchingFailed of error: string
        | LoadSelfInfo
        | SelfInfoFetched of selfInfo: Result<UserInfoResponse,string>
        | SelfInfoFetchingFailed of error: string
        | LoadDialogs
        | DialogsFetched of dialogs: Result<ChatItem[],string>
        | DialogsFetchingFailed of error: string
        | DialogsFiltered of dialogsFiltered: ChatItem[]
        | RemoveTyping of pk: string
        | AddTyping of pk: string
        | ChangeOnline of pk: string * onoff: bool
        | AddMessage of msg: MessageBox
        | MessageIdChanged of old_int: int64 * new_id: int64
        | UnreadCountChanged of id: string * count: int
        | PerformSendingMessage of text: string
        | PerformFileUpload of Browser.Types.FileList
        | SetShowNewChatPopup of show: bool
        | SelectDialog of dialog: ChatItem
        | FileUploadError of error: string
        | FileUploadSuccess of uploadRes: Result<MessageModelFile,string>
        | SetMsgIdAsRead of msg_id: int64

    let init () =
        let cmd = [| Msg.LoadMessages
                     Msg.LoadDialogs
                     Msg.LoadSelfInfo
                  |] |> Array.map Cmd.ofMsg |> Cmd.batch
        {
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
        socket = RWebSocket.Create("ws://" + Browser.Dom.window.location.host + "/chat_ws")}, cmd

    let update (msg: Msg) (state: State) =

        JS.console.log "Update cycle received msg: "
        printfn "%A" msg
        JS.console.log "And State: "
        JS.console.log state
        match msg with
        | SocketConnectionStateChanged socketState ->
            printfn $"SocketState changed to {socketState}"
            state, Cmd.none
        | UsersDataFetchingFailed err ->
            printfn $"Failed to fetch users -  {err}"
            toast.error(err)
            {state with UsersDataLoading = false}, Cmd.none
        | UsersDataFetched usersResults ->
            match usersResults with
            | Ok users ->
                printfn "Fetched users"
                JS.console.log(users)
                {state with AvailableUsers=users; UsersDataLoading = false},Cmd.none
            | Error s -> state, Cmd.ofMsg (UsersDataFetchingFailed s)
        | LoadUsersData ->
            let cmd = Cmd.OfPromise.either
                          Logic.fetchUsersList // Promise
                          state.dialogList // Argument
                          Msg.UsersDataFetched // Map Success
                          (fun x -> UsersDataFetchingFailed (x.ToString())) // Map Exception
            {state with UsersDataLoading = true}, cmd

        | MessagesFetchingFailed err ->
            printfn $"Failed to fetch messages - {err}"
            toast.error(err)
            state, Cmd.none

        | MessagesFetched messagesResults ->
            match messagesResults with
            | Ok messages ->
                printfn $"Fetched messages"
                JS.console.log(messages)
                {state with messageList=messages},Cmd.none
            | Error s -> state, Cmd.ofMsg (MessagesFetchingFailed s)

        | LoadMessages ->
            let cmd = Cmd.OfPromise.either
                          Logic.fetchMessages // Promise
                          () // Argument
                          Msg.MessagesFetched // Map Success
                          (fun x -> Msg.MessagesFetchingFailed (x.ToString())) // Map Exception
            state, cmd

        | DialogsFetchingFailed err ->
            printfn $"Failed to fetch dialogs - {err}"
            toast.error(err)
            state, Cmd.none

        | DialogsFetched dialogsResults ->
            match dialogsResults with
            | Ok dialogs ->
                printfn $"Fetched dialogs"
                JS.console.log(dialogs)
                let cmd =
                    match Array.tryHead dialogs with
                    | Some d -> Msg.SelectDialog d |> Cmd.ofMsg
                    | None -> Cmd.none

                {state with dialogList=dialogs; filteredDialogList=dialogs},cmd

            | Error s -> state, Cmd.ofMsg (DialogsFetchingFailed s)

        | LoadDialogs ->
            let cmd = Cmd.OfPromise.either
                          Logic.fetchDialogs // Promise
                          () // Argument
                          Msg.DialogsFetched // Map Success
                          (fun x -> Msg.DialogsFetchingFailed (x.ToString())) // Map Exception
            state, cmd


        | SelfInfoFetchingFailed err ->
            printfn $"Failed to fetch selfInfo - {err}"
            toast.error(err)
            state, Cmd.none

        | SelfInfoFetched selfInfoResults ->
            match selfInfoResults with
            | Ok selfInfo ->
                printfn $"Fetched selfInfo {selfInfo}"
                {state with selfInfo=Some selfInfo},Cmd.none
            | Error s -> state, Cmd.ofMsg (SelfInfoFetchingFailed s)

        | LoadSelfInfo ->
            let cmd = Cmd.OfPromise.either
                          Logic.fetchSelfInfo // Promise
                          () // Argument
                          Msg.SelfInfoFetched // Map Success
                          (fun x -> Msg.SelfInfoFetchingFailed (x.ToString())) // Map Exception
            state, cmd

        | DialogsFiltered dialogs ->
            {state with filteredDialogList = dialogs }, Cmd.none

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
        | AddMessage msg ->
            printfn "Calling addMessage for"
            JS.console.log msg
            let mutable actualNewMsg = msg
            if not msg.data.out && msg.data.message_id > 0 && state.selectedDialog.IsSome then
                if state.selectedDialog.Value.id = msg.data.dialog_id then
                    Logic.sendMessageReadMessage state.socket msg.data.dialog_id msg.data.message_id
                    actualNewMsg <- {msg with status = MessageBoxStatus.Read}


            let newMessageList =  [| actualNewMsg |] |> Array.append state.messageList

            let mutable doesntNeedLastMessageSet = false

            let mutable newDialogList = state.dialogList

            if not msg.data.out then
                // let hasDialogAlready = dialogs.some((e) => e.id === msg.data.dialog_id);
                let hasDialogAlready = state.dialogList
                                       |> Array.exists (fun e -> e.id = msg.data.dialog_id)
                if not hasDialogAlready then
                    let d = Logic.createNewDialogModelFromIncomingMessageBox(msg)
                    newDialogList <- [| d |] |> Array.append state.dialogList
                    doesntNeedLastMessageSet <- true

            if not doesntNeedLastMessageSet then
                newDialogList <-
                    newDialogList
                    |> Array.map (fun el ->
                        if el.id = msg.data.dialog_id then
                            printfn $"Setting dialog {msg.data.dialog_id} last message"
                            {el with subtitle = Logic.getSubtitleTextFromMessageBox (Some msg)}
                        else
                            el
                        )

            {state with messageList = newMessageList
                        dialogList = newDialogList
                        filteredDialogList = newDialogList }, Cmd.none

        | SelectDialog dialog ->
            printfn $"Selecting dialog {dialog.id}"
            let newDialogList =
                state.dialogList
                |> Array.map (fun dlg ->
                    if dlg.id = dialog.id then
                        { dlg with statusColorType = Some "encircle" }
                    else
                        { dlg with statusColorType = None }
                    )

            let readMsgCmd =
                Logic.markMessagesForDialogAsRead state.socket dialog state.messageList
                |> Array.map (Msg.SetMsgIdAsRead)
                |> Array.map Cmd.ofMsg
                |> Cmd.batch

            {state with dialogList = newDialogList
                        selectedDialog = Some dialog
                        //TODO: is resetting filtered needed ?
                        filteredDialogList = newDialogList}, readMsgCmd

        | RemoveTyping pk ->
            printfn $"Removing {pk} from typing pk-s"
            let newTypingPks = state.typingPKs |> Array.filter (fun x -> x <> pk)
            {state with typingPKs = newTypingPks}, Cmd.none

        | AddTyping pk ->
            printfn $"Adding {pk} to typing pk-s"
            let newTypingPks = [| pk |] |> Array.append state.typingPKs
            let cmd p = promise {
                do! Promise.sleep TYPING_TIMEOUT
                return p
            }
            {state with typingPKs = newTypingPks}, Cmd.OfPromise.perform cmd pk (Msg.RemoveTyping)

        | ChangeOnline (pk, onoff) ->
            printfn $"""Setting {pk} to {if onoff then "online" else "offline" } status"""
            let newOnlines =
                if onoff then
                    [|pk|] |> Array.append state.onlinePKs
                else
                    state.onlinePKs |> Array.filter (fun x -> x <> pk)

            let newDialogList =
                state.dialogList
                |> Array.map (fun dlg ->
                    if dlg.id = pk then
                        if onoff then
                            { dlg with statusColor = "lightgreen" }
                        else
                            { dlg with statusColor = "" }
                    else
                        dlg
                    )

            {state with
                     dialogList = newDialogList
                     onlinePKs = newOnlines
                     //TODO: is resetting filtered needed ?
                     filteredDialogList = newDialogList}, Cmd.none

        | MessageIdChanged (old_id, new_id) ->
            printfn $"Replacing random id {old_id} with db_id {new_id}"
            let newMsgList =
                state.messageList
                |> Array.map (fun msg ->
                    if msg.data.message_id = old_id then
                        if msg.data.out then
                            {msg with status = MessageBoxStatus.Sent; data = {msg.data with message_id = new_id}}
                        else
                            match (state.selectedDialog |> Option.map (fun x -> x.id)) with
                            | Some pk when pk = msg.data.dialog_id ->
                                Logic.sendMessageReadMessage state.socket pk  new_id
                                {msg with status = MessageBoxStatus.Read; data = {msg.data with message_id = new_id}}

                            | _ -> {msg with status = MessageBoxStatus.Received; data = {msg.data with message_id = new_id}}

                    else
                        msg
                    )
            {state with messageList = newMsgList}, Cmd.none

        | UnreadCountChanged (dialog_id, count) ->
            printfn $"Got new unread count {count} for dialog {dialog_id}"

            let mappingFn dlg =
                if dlg.id = dialog_id then
                    printfn $"Setting new unread count {count} for dialog {dialog_id}"
                    { dlg with unread = count }
                else
                    dlg

            let newDialogList =
                state.dialogList
                |> Array.map mappingFn
            let newSelectedDialog =
                state.selectedDialog
                |> Option.map mappingFn
            {state with
                     dialogList = newDialogList
                     selectedDialog = newSelectedDialog
                     //TODO: is resetting filtered needed ?
                     filteredDialogList = newDialogList}, Cmd.none

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
                            (Logic.uploadFile files) // Promise
                            cookie // Argument
                            Msg.FileUploadSuccess // Map Success
                            (fun x -> Msg.FileUploadError (x.ToString())) // Map Exception
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
            toast.error(err)
            state, Cmd.none

module private Funcs =
    open Elmish
    open Browser.Types


    let getConnectionStateText (socketState: WebSocketState) =
        match socketState with
        | WebSocketState.CONNECTING -> "Connecting..."
        | WebSocketState.OPEN -> "Connected"
        | WebSocketState.CLOSING -> "Disconnecting..."
        | WebSocketState.CLOSED -> "Disconnected"
        | _ -> "Unknown"


    let isTyping (socket: WebSocket) :unit -> unit=
        lodash_throttle((fun () -> Logic.sendIsTypingMessage socket),float TYPING_TIMEOUT)

    let localSearch (searchInputRef: IRefValue<HTMLInputElement option>) (state: Elmish.State) dispatch :unit -> unit =
        lodash_throttle((fun () ->
            let value = searchInputRef.current |> Option.map (fun x -> x.value)
            printfn $"Localsearch with {value}"
            match value with
            | Some v ->
                let newDialogList = state.dialogList
                                    |> Array.filter (fun x -> x.title.ToLower().Contains(v.ToLower()))
                dispatch (Msg.DialogsFiltered newDialogList)
            | None -> dispatch (Msg.DialogsFiltered state.filteredDialogList)

            ),100)

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
    let CustomChatList className dataSource onClick =
        let className = $"""rce-container-clist {className |> Option.defaultValue ""}"""

        let chatItems = dataSource
                        // |> Seq.sortByDescending (fun x ->  x.date)
                        |> Seq.mapi (fun i x ->
            JSX.jsx $"""
            <ChatItemR
            {JsInterop.emitJsExpr () "...x"}
            key={i}
            onClick={fun (_) -> onClick |> Option.iter (fun z -> z(x,i))}
            />
            """
            )
        let style = JsInterop.createObj [ ("display", "block"); ("overflow","auto") ]

        JSX.jsx $"""
        import {{ ChatItem as ChatItemR }} from "react-chat-elements"
        <div className={className} style={style}>
          {chatItems}
        </div>
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
        let localSearch = Funcs.localSearch searchInputRef model dispatch
        // JS.console.log(model.filteredDialogList)

        let customChatListFunction =  fun (item, i: int) ->
                        // JS.console.log(item, i )
                        dispatch (Msg.SelectDialog item)

        let customChatList = CustomChatList None
                                model.filteredDialogList
                                (Some customChatListFunction)


        let sidebarTop = JSX.jsx $"""
            <span className='chat-list'>
                <Input
                    placeholder="Search..."
                    referance={searchInputRef}
                    onKeyPress={fun (e: KeyboardEvent) ->
                      if e.charCode <> 13 then
                        localSearch()
                        false
                      elif e.charCode = 13 then
                        localSearch()
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
                                localSearch()
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
                {customChatList}
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
                // JS.console.log(item, i , e)
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
            import {{ Navbar, ChatItem as ChatItemR }} from "react-chat-elements"
            import {{ FaEdit }} from "react-icons/fa"
            <Navbar
            left={{
                <ChatItemR
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
    let callbacks: Logic.WSHandlingCallbacks = {
        addMessage = (fun msg -> dispatch (Elmish.Msg.AddMessage msg))
        replaceMessageId = (fun (old_id, new_id) -> dispatch (Elmish.Msg.MessageIdChanged (old_id, new_id)))
        addPKToTyping = (fun pk -> dispatch (Elmish.Msg.AddTyping pk))
        changePKOnlineStatus = (fun (pk, onoff) -> dispatch (Elmish.Msg.ChangeOnline (pk, onoff)))
        setMessageIdAsRead = (fun msg_id -> dispatch (Elmish.Msg.SetMsgIdAsRead msg_id))
        newUnreadCount = (fun (id, count) -> dispatch (Elmish.Msg.UnreadCountChanged (id, count)))
    }

    React.useEffect ((fun () ->
        printfn "Running socket setup..."
        let socket = model.socket
        socket.onopen <-
            fun (e) ->
                printfn "Connected!"
                App.Utils.toast.success("Connected!", toastOptions)
                dispatch (Elmish.Msg.SocketConnectionStateChanged socket.readyState)
        socket.onclose <-
            fun (e) ->
                printfn "Disconnected!"
                App.Utils.toast.info("Disconnected...", toastOptions)
                dispatch (Elmish.Msg.SocketConnectionStateChanged socket.readyState)
        socket.onmessage <-
            fun (e) ->
                printfn "Received websocket message"
                dispatch (Elmish.Msg.SocketConnectionStateChanged socket.readyState)

                let err = Logic.handleIncomingWebsocketMessage socket (e.data :?> string) callbacks
                err |> Option.iter (App.Utils.toast.error)
    ), [| box model.socket ; box dispatch |])

    let filteredMessages =
        match model.selectedDialog with
        | Some dialog -> model.messageList |> Array.filter (fun m -> m.data.dialog_id = dialog.id)
        | None -> Array.empty


    let popUp =  {|
                   show=model.ShowNewChatPopup
                   header = "New chat"
                   headerButtons =
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

                   renderContent = fun () ->
                        if model.UsersDataLoading then
                            JSX.jsx "<div><p>Loading data...</p></div>"
                        else
                            if model.AvailableUsers.Length = 0 then
                                JSX.jsx "<div><p>No users available</p></div>"
                            else
                                Components.PopUpRightPanel model dispatch
                 |}


    JS.console.log("filtered messages:")
    JS.console.log(filteredMessages)
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
            <Popup popup = {popUp}  />

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
                dataSource={filteredMessages}/>

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
