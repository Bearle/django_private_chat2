namespace App
module Utils =
    open System
    open Fable.Core.JsInterop
    open Fable.Core
    open Feliz

    // https://stackoverflow.com/a/22429679

    let inline toJsx (el: ReactElement) : JSX.Element = unbox el
    let inline toReact (el: JSX.Element) : ReactElement = unbox el

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

    let humanFileSize (size: int) =
        let i = JS.Math.floor(JS.Math.log(float size) / JS.Math.log(1024.))
        let r = (float size / JS.Math.pow(1024.,i))
        let suffix = [|"B";"kB"; "MB";"GB";"TB"|].[int i]
        $"%.2f{r} %s{suffix}"

    let getCookie() =
        let name = "csrftoken"
        let mutable cookieValue = None

        let mutable Break = false
        if not (String.IsNullOrWhiteSpace Browser.Dom.document.cookie) && Browser.Dom.document.cookie <> "" then

            let cookies = Browser.Dom.document.cookie.Split(';')
            for c in cookies do
                if not Break then
                    let cookie = c.Trim()
                    if cookie.Substring(0, name.Length + 1) = (name + "=") then
                        cookieValue <- Some (JS.decodeURIComponent(cookie.Substring(name.Length + 1)))
                        Break <- true


        cookieValue
