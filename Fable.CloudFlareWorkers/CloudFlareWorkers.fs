module CloudFlareWorkers

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

type IHttpRequest = interface end

type IHttpResponse = interface end

[<StringEnum; RequireQualifiedAccess>]
type HttpMethod =
    | [<CompiledName "GET">] GET
    | [<CompiledName "POST">] POST
    | [<CompiledName "PUT">] PUT
    | [<CompiledName "DELETE">] DELETE
    | [<CompiledName "PATCH">] PATCH
    | [<CompiledName "OPTIONS">] OPTIONS

/// Interop utilities used in the library
module internal Interop =
    [<Emit("Array.from($0.headers.entries())")>]
    let headers (request: obj) : (string * string) [] = jsNative

    [<Emit("addEventListener('fetch', $0)")>]
    let addEventHandler (f: obj -> obj) : unit = jsNative

    [<Emit("new Response($0, $1)")>]
    let createResponse (body: string, options: obj) = jsNative

    [<Emit("$0 in $1")>]
    let hasKey (key: string) (value: obj) = jsNative

    [<Emit("delete $1[$0]")>]
    let deleteKey (key: string) (value: obj) = jsNative

    [<Emit("$2[$0] = $1")>]
    let set (key: string) (value: obj) (object: obj) : unit = jsNative

    [<Emit("new Uint8Array($0)")>]
    let createUInt8Array (x: obj) : byte[] = jsNative

/// Contains functions for working with HTTP requests
module Request =
    /// Reads the headers of the incoming request
    let headers (request: IHttpRequest) : Map<string, string> =
        Map.ofArray (Interop.headers request)

    /// Reads the body content of the incoming request
    let body (request: IHttpRequest) : Async<string> =
        async {
            let! text = Async.AwaitPromise (request?text())
            return text
        }

    [<Emit("new URL($0.url).pathname")>]
    /// Returns the URL of the incoming request
    let path (request: IHttpRequest) : string = jsNative
    [<Emit("$0.method")>]
    /// Returns the HTTP method of the request
    let method (request: IHttpRequest) : HttpMethod = jsNative
    /// Returns the path of the request as segmented list of strings
    let pathSegments (request: IHttpRequest) : string list =
        let segments = path request
        segments.Split('/')
        |> List.ofArray
        |> List.filter (String.IsNullOrWhiteSpace >> not)



[<AutoOpen>]
module Extensions =
    type IHttpRequest with
        member request.headers() : Map<string, string> =
            Map.ofArray (Interop.headers request)
        /// Returns the URL of the incoming request
        member request.path = Request.path request
        /// Returns the HTTP method of the request
        member request.method = Request.method request
        /// Returns the path of the request as segmented list of strings
        member request.pathSegments = Request.pathSegments request
        /// Reads the body content of the incoming request as text
        member request.body() = Request.body request
        member request.formData() : Async<FormData> =
            async {
                let! formData = Async.AwaitPromise(request?formData())
                return formData
            }

        member request.blob() : Async<Blob> =
            async {
                let! text = Async.AwaitPromise (request?blob())
                return text
            }

        member request.rawBody() : Async<byte[]> =
            async {
                let! arrayBuffer = Async.AwaitPromise(request?arrayBuffer())
                return Interop.createUInt8Array arrayBuffer
            }

    type IHttpResponse with
        /// Reads the body content of the response as text
        member response.body() : Async<string> =
            async {
                let! text = Async.AwaitPromise (response?text())
                return text
            }

        member response.blob() : Async<Blob> =
            async {
                let! text = Async.AwaitPromise (response?blob())
                return text
            }

        member response.formData() : Async<FormData> =
            async {
                let! formData = Async.AwaitPromise(response?formData())
                return formData
            }

        member response.headers() : Map<string, string> =
            Map.ofArray (Interop.headers response)

        member response.rawBody() : Async<byte[]> =
            async {
                let! arrayBuffer = Async.AwaitPromise(response?arrayBuffer())
                return Interop.createUInt8Array arrayBuffer
            }

/// Utilities for working with HTTP responses
[<CompiledName "ResponseModule">]
type Response =
    /// Creates a new response object
    static member create (?body:string, ?status:int, ?headers: (string * string) list, ?statusText:string) : IHttpResponse =
        let options = obj()
        status |> Option.iter (fun value -> Interop.set "status" value options)
        statusText |> Option.iter (fun value -> Interop.set "statusText" value options)
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!value)) options)
        Interop.createResponse(!!body, options)

type Worker =
    static member initialize (handler: IHttpRequest -> IHttpResponse) : unit =
        Interop.addEventHandler (fun fetchEvent -> fetchEvent?respondWith(handler fetchEvent?request))

    static member initialize (handler: IHttpRequest -> Async<IHttpResponse>) : unit =
        Interop.addEventHandler (fun fetchEvent -> fetchEvent?respondWith(Async.StartAsPromise(handler fetchEvent?request)))