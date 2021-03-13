import React, {Component} from 'react';
import 'react-chat-elements/dist/main.css';
import 'react-toastify/dist/ReactToastify.css';
import {ToastContainer, toast} from 'react-toastify';
import {
    MessageBox,
    ChatItem,
    ChatList,
    SystemMessage,
    MessageList,
    Input,
    Button,
    Avatar,
    Navbar,
    SideBar,
    Dropdown,
    Popup,
} from 'react-chat-elements';
import throttle from 'lodash.throttle';
import {FaSearch, FaComments, FaWindowClose as FaClose, FaSquare, FaTimesCircle} from 'react-icons/fa';
import {MdMenu} from 'react-icons/md';
import ReconnectingWebSocket from 'reconnecting-websocket';
import {
    fetchSelfInfo,
    handleIncomingWebsocketMessage,
    sendOutgoingTextMessage,
    filterMessagesForDialog,
    fetchDialogs,
    fetchMessages,
    sendIsTypingMessage,
    hash64,
    getPhotoString
} from "../fs-src/App.fs.js"
import {
    format,
} from 'timeago.js';

import loremIpsum from 'lorem-ipsum';


export class App extends Component {

    constructor(props) {
        super(props);
        // Refs
        this.textInput = null;
        this.setTextInputRef = element => {
            this.textInput = element;
        };
        this.clearTextInput = () => {
            if (this.textInput) this.textInput.clear();
        };

        this.searchInput = null;
        this.setSearchInputRef = element => {
            this.searchInput = element;
        };
        this.clearSearchInput = () => {
            if (this.searchInput) this.searchInput.clear();
        };


        this.state = {
            socketConnectionState: 0,
            messageList: [],
            dialogList: [],
            selfInfo: null,
            selectedDialog: null,
            socket: new ReconnectingWebSocket('ws://' + window.location.host + '/chat_ws')
        };
        //some js magic
        this.performSendingMessage = this.performSendingMessage.bind(this);
        this.addMessage = this.addMessage.bind(this);
        this.replaceMessageId = this.replaceMessageId.bind(this);

        this.deb = throttle(() => {
            sendIsTypingMessage(this.state.socket)
        }, 5000)
    }

    componentDidMount() {
        fetchMessages().then((r) => {
            if (r.tag === 0) {
                console.log("Fetched messages:")
                console.log(r.fields[0])
                this.setState({messageList: r.fields[0]})
            } else {
                console.log("Messages error:")
                toast.error(r.fields[0])
            }
        })

        fetchDialogs().then((r) => {
            if (r.tag === 0) {
                console.log("Fetched dialogs:")
                console.log(r.fields[0])
                this.setState({dialogList: r.fields[0]})
                this.selectDialog(r.fields[0][0])
            } else {
                console.log("Dialogs error:")
                toast.error(r.fields[0])
            }
        })
        fetchSelfInfo().then((r) => {
            if (r.tag === 0) {
                console.log("Fetched selfInfo:")
                console.log(r.fields[0])
                this.setState({selfInfo:r.fields[0]})
            } else {
                console.log("SelfInfo error:")
                toast.error(r.fields[0])
            }
        })
        this.setState({socketConnectionState: this.state.socket.readyState});
        const that = this;
        let socket = this.state.socket;
        let toastOptions = {
            autoClose: 1500,
            hideProgressBar: true,
            closeOnClick: false,
            pauseOnHover: false,
            pauseOnFocusLoss: false,
            draggable: false,
        };

        socket.onopen = function (e) {
            toast.success("Connected!", toastOptions)
            that.setState({socketConnectionState: socket.readyState});
        }
        socket.onmessage = function (e) {
            that.setState({socketConnectionState: socket.readyState});
            handleIncomingWebsocketMessage(socket, e.data, that.addMessage, that.replaceMessageId)
        };
        socket.onclose = function (e) {
            toast.info("Disconnected...", toastOptions)
            that.setState({socketConnectionState: socket.readyState});
            console.log("websocket closed")
        }
    }

    selectDialog(item) {
        this.setState({selectedDialog: item})
        this.setState(prevState => ({
            dialogList: prevState.dialogList.map(el => (el.id === item.id ?
                {...el, statusColorType: 'encircle'} : {...el, statusColorType: undefined}))
        }))
    }

    getSocketState() {
        if (this.state.socket.readyState === 0) {
            return "Connecting..."
        } else if (this.state.socket.readyState === 1) {
            return "Connected"
        } else if (this.state.socket.readyState === 2) {
            return "Disconnecting..."
        } else if (this.state.socket.readyState === 3) {
            return "Disconnected"
        }
    }

    getRandomColor() {
        var letters = '0123456789ABCDEF';
        var color = '#';
        for (var i = 0; i < 6; i++) {
            color += letters[Math.floor(Math.random() * 16)];
        }
        return color;
    }

    token() {
        return (parseInt(Math.random() * 10 % 8));
    }

    photo(size) {
        return getPhotoString(String(Math.random()) + String(Math.random()), size);
    }

    random(type, mtype) {
        switch (type) {
            case 'message':
                mtype = mtype || this.token();
                var status = 'waiting';
                switch (mtype) {
                    case 0:
                        mtype = 'photo';
                        status = 'sent';
                        break;
                    case 1:
                        mtype = 'file';
                        status = 'sent';
                        break;
                    case 2:
                        mtype = 'system';
                        status = 'received';
                        break;
                    case 3:
                        mtype = 'location';
                        break;
                    case 4:
                        mtype = 'spotify';
                        break;
                    case 5:
                        mtype = 'meeting';
                        break;
                    case 6:
                        mtype = 'video';
                        status = 'sent';
                        break;
                    case 7:
                        mtype = 'audio';
                        break;
                    default:
                        mtype = 'text';
                        status = 'read';
                        break;
                }

                return {
                    position: (this.token() >= 1 ? 'right' : 'left'),
                    forwarded: true,
                    replyButton: true,
                    reply: this.token() >= 1 ? ({
                        photoURL: this.token() >= 1 ? `${this.photo(150)}` : null,
                        title: loremIpsum({count: 2, units: 'words'}),
                        titleColor: this.getRandomColor(),
                        message: loremIpsum({count: 1, units: 'sentences'}),
                    }) : null,
                    meeting: this.token() >= 1 ? ({
                        subject: loremIpsum({count: 2, units: 'words'}),
                        title: loremIpsum({count: 2, units: 'words'}),
                        date: +new Date(),
                        collapseTitle: loremIpsum({count: 2, units: 'words'}),
                        participants: Array(this.token() + 6).fill(1).map(x => ({
                            id: parseInt(Math.random() * 10 % 7),
                            title: loremIpsum({count: 1, units: 'words'}),
                        })),
                        dataSource: Array(this.token() + 5).fill(1).map(x => ({
                            id: String(Math.random()),
                            avatar: `${this.photo()}`,
                            message: loremIpsum({count: 1, units: 'sentences'}),
                            title: loremIpsum({count: 2, units: 'words'}),
                            avatarFlexible: true,
                            date: +new Date(),
                            event: {
                                title: loremIpsum({count: 2, units: 'words'}),
                                avatars: Array(this.token() + 2).fill(1).map(x => ({
                                    src: `${this.photo()}`,
                                    title: "react, rce"
                                })),
                                avatarsLimit: 5,
                            },
                            record: {
                                avatar: `${this.photo()}`,
                                title: loremIpsum({count: 1, units: 'words'}),
                                savedBy: 'Kaydeden: ' + loremIpsum({count: 2, units: 'words'}),
                                time: new Date().toLocaleString(),
                            },
                        })),
                    }) : null,
                    type: mtype,
                    theme: 'white',
                    view: 'list',
                    title: loremIpsum({count: 2, units: 'words'}),
                    titleColor: this.getRandomColor(),
                    text: mtype === 'spotify' ? 'spotify:track:0QjjaCaXE45mvhCnV3C0TA' : loremIpsum({
                        count: 1,
                        units: 'sentences'
                    }),
                    data: {
                        videoURL: this.token() >= 1 ? 'https://www.w3schools.com/html/mov_bbb.mp4' : 'http://www.exit109.com/~dnn/clips/RW20seconds_1.mp4',
                        audioURL: 'https://www.w3schools.com/html/horse.mp3',
                        uri: `${this.photo(150)}`,
                        status: {
                            click: true,
                            loading: 0.5,
                            download: mtype === 'video',
                        },
                        size: "100MB",
                        width: 300,
                        height: 300,
                        latitude: '37.773972',
                        longitude: '-122.431297',
                        staticURL: 'https://api.mapbox.com/styles/v1/mapbox/streets-v11/static/pin-s-circle+FF0000(LONGITUDE,LATITUDE)/LONGITUDE,LATITUDE,ZOOM/270x200@2x?access_token=KEY',
                    },
                    onLoad: () => {
                        console.log('Photo loaded');
                    },
                    status: status,
                    date: +new Date(),
                    onReplyMessageClick: () => {
                        console.log('onReplyMessageClick');
                    },
                    avatar: `${this.photo()}`,
                };
            case 'chat':
                return {
                    id: String(Math.random()),
                    avatar: `${this.photo()}`,
                    avatarFlexible: true,
                    statusColor: 'lightgreen',
                    statusColorType: parseInt(Math.random() * 100 % 2) === 1 ? 'encircle' : undefined,
                    alt: loremIpsum({count: 2, units: 'words'}),
                    title: loremIpsum({count: 2, units: 'words'}),
                    date: new Date(),
                    subtitle: loremIpsum({count: 1, units: 'sentences'}),
                    unread: parseInt(Math.random() * 10 % 3),
                    dropdownMenu: (
                        <Dropdown
                            animationPosition="norteast"
                            title='Dropdown Title'
                            buttonProps={{
                                type: "transparent",
                                color: "#cecece",
                                icon: {
                                    component: <MdMenu/>,
                                    size: 24,
                                }
                            }}
                            items={[
                                {
                                    icon: {
                                        component: <FaSquare/>,
                                        float: 'left',
                                        color: 'red',
                                        size: 22,
                                    },
                                    text: 'Menu Item'
                                },
                                {
                                    icon: {
                                        component: <FaSquare/>,
                                        float: 'left',
                                        color: 'purple',
                                        size: 22,
                                    },
                                    text: 'Menu Item'
                                },
                                {
                                    icon: {
                                        component: <FaSquare/>,
                                        float: 'left',
                                        color: 'yellow',
                                        size: 22,
                                    },
                                    text: 'Menu Item'
                                },
                            ]}/>
                    ),
                };
            case 'meeting':
                return {
                    id: String(Math.random()),
                    lazyLoadingImage: `${this.photo()}`,
                    avatarFlexible: true,
                    subject: loremIpsum({count: 1, units: 'sentences'}),
                    date: new Date(),
                    avatars: Array(this.token() + 2).fill(1).map(x => ({
                        src: `${this.photo()}`,
                    })),
                    closable: true,
                };
        }
    }

    addMessage(msg) {
        console.log("Calling addMessage for ")
        console.log(msg)
        let list = this.state.messageList;
        list.push(msg);
        this.setState({
            messageList: list,
        });
    }

    replaceMessageId(old_id, new_id) {
        console.log("Replacing random id  " + old_id + " with db_id " + new_id)
        this.setState(prevState => ({
            messageList: prevState.messageList.map(function (el) {
                if (el.data.message_id.Equals(old_id)) {
                    let new_status = el.data.out ? 'sent' : 'received'
                    return {...el, data: {dialog_id: el.data.dialog_id, message_id: new_id}, status: new_status}
                } else {
                    return el
                }
            })
        }))
        console.log(this.state)
    }

    performSendingMessage() {
        if (this.state.selectedDialog) {
            let text = this.textInput.input.value;
            let user_pk = this.state.selectedDialog.id;
            this.clearTextInput();
            let msgBox = sendOutgoingTextMessage(this.state.socket, text, user_pk, this.state.selfInfo);
            console.log("sendOutgoingTextMessage result:")
            console.log(msgBox)
            if (msgBox) {
                this.addMessage(msgBox);
            }
        }
    }

    render() {
        return (
            <div className='container'>
                <div
                    className='chat-list'>
                    <SideBar
                        type='light'
                        top={
                            <span className='chat-list'>
                                <Input
                                    placeholder="Search..."
                                    ref={this.setSearchInputRef}
                                    rightButtons={
                                        <div>
                                            <Button
                                                type='transparent'
                                                color='black'
                                                onClick={() => console.log("search invoke with" + this.searchInput.input.value)}
                                                icon={{
                                                    component: <FaSearch/>,
                                                    size: 18
                                                }}/>
                                            <Button
                                                type='transparent'
                                                color='black'
                                                icon={{
                                                    component: <FaTimesCircle/>,
                                                    size: 18
                                                }}
                                                onClick={() => this.clearSearchInput()}/>
                                        </div>
                                    }
                                />

                                <ChatList onClick={(item, i, e) => this.selectDialog(item)}
                                          dataSource={this.state.dialogList}/>
                            </span>

                        }
                        bottom={
                            <Button type='transparent' color='black' disabled={true}
                                    text={"Connection state: " + this.getSocketState()}/>
                        }/>
                </div>
                <div
                    className='right-panel'>
                    <ToastContainer/>
                    <MessageList
                        className='message-list'
                        lockable={true}
                        downButtonBadge={10}
                        dataSource={filterMessagesForDialog(this.state.selectedDialog, this.state.messageList)}/>

                    <Input
                        placeholder="Type here to send a message."
                        defaultValue=""
                        ref={this.setTextInputRef}
                        multiline={true}
                        // buttonsFloat='left'
                        onKeyPress={(e) => {
                            if (e.charCode !== 13) {
                                console.log('key pressed');

                                this.deb();
                                // this.debouncedTyping();
                            }
                            if (e.shiftKey && e.charCode === 13) {
                                return true;
                            }
                            if (e.charCode === 13) {
                                this.performSendingMessage()
                                e.preventDefault();
                                return false;
                            }
                        }}
                        rightButtons={
                            <Button
                                text='Send'
                                onClick={() => this.performSendingMessage()}/>
                        }/>
                </div>
            </div>
        );
    }
}

export default App;
