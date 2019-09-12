module CloudFlareWorkers

open System
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types

type IHttpRequest = interface end

type IHttpResponse = interface end

type IRequestContext =
    abstract request : IHttpRequest
    abstract fetch : IHttpRequest -> Async<IHttpResponse>

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

    [<Emit("new Request($0, $1)")>]
    let createRequest (url: string, options: obj) = jsNative

    [<Emit("$0 in $1")>]
    let hasKey (key: string) (value: obj) = jsNative

    [<Emit("delete $1[$0]")>]
    let deleteKey (key: string) (value: obj) = jsNative

    [<Emit("$2[$0] = $1")>]
    let set (key: string) (value: obj) (object: obj) : unit = jsNative

    [<Emit("new Uint8Array($0)")>]
    let createUInt8Array (x: obj) : byte[] = jsNative
    [<Emit("new Blob([$0.buffer], { 'type': 'application/octet-stream' })")>]
    let createBlobFromBytes (bytes: byte[]) : Blob = jsNative

    [<Emit("fetch($0)")>]
    let fetch (request: IHttpRequest) : JS.Promise<IHttpResponse> = jsNative

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
    /// Returns the path of the incoming request
    let path (request: IHttpRequest) : string = jsNative
    [<Emit("$0.url")>]
    /// Returns the URL of the incoming request
    let url (request: IHttpRequest) : string = jsNative
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
        /// Returns the path of the incoming request
        member request.path = Request.path request
        /// Returns the URL of the incoming request
        member request.url = Request.url request
        /// Returns the HTTP method of the request
        member request.method = Request.method request
        /// Returns the path of the request as segmented list of strings
        member request.pathSegments = Request.pathSegments request
        /// Reads the body content of the incoming request as text
        member request.body() = Request.body request
        /// Reads the request body as form data
        member request.formData() : Async<FormData> =
            async {
                let! formData = Async.AwaitPromise(request?formData())
                return formData
            }
        /// Reads the body of the request as Blob of raw data
        member request.blob() : Async<Blob> =
            async {
                let! text = Async.AwaitPromise (request?blob())
                return text
            }
        /// Reads the body of the request as raw data of bytes (byte array)
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

        /// Reads the body of the request as Blob of raw data
        member response.blob() : Async<Blob> =
            async {
                let! text = Async.AwaitPromise (response?blob())
                return text
            }

        /// Reads the body of the response as form data
        member response.formData() : Async<FormData> =
            async {
                let! formData = Async.AwaitPromise(response?formData())
                return formData
            }

        /// Reads the headers of the response
        member response.headers() : Map<string, string> =
            Map.ofArray (Interop.headers response)

        /// Reads the body of the response as raw data of bytes (byte array)
        member response.rawBody() : Async<byte[]> =
            async {
                let! arrayBuffer = Async.AwaitPromise(response?arrayBuffer())
                return Interop.createUInt8Array arrayBuffer
            }


[<CompiledName "RequestModule">]
type Request =
    static member create (url:string, ?body: string, ?headers: Map<string, string>) : IHttpRequest =
        let options = obj()
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        body |> Option.iter (fun value -> Interop.set "body" value options)
        Interop.createResponse(url, options)

    static member create (url:string, ?body: Blob, ?headers: Map<string, string>) : IHttpRequest =
        let options = obj()
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        body |> Option.iter (fun value -> Interop.set "body" value options)
        Interop.createResponse(url, options)

    static member create (url:string, ?body: byte[], ?headers: Map<string, string>) : IHttpRequest =
        let options = obj()
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        body |> Option.iter (fun value -> Interop.set "body" (Interop.createBlobFromBytes value) options)
        Interop.createResponse(url, options)

    static member create (url:string, ?body: FormData, ?headers: Map<string, string>) : IHttpRequest =
        let options = obj()
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        body |> Option.iter (fun value -> Interop.set "body" value options)
        Interop.createResponse(url, options)

/// Utilities for working with HTTP responses
[<CompiledName "ResponseModule">]
type Response =
    /// Creates a new response object
    static member create (?body:string, ?status:int, ?headers: Map<string, string>, ?statusText:string) : IHttpResponse =
        let options = obj()
        status |> Option.iter (fun value -> Interop.set "status" value options)
        statusText |> Option.iter (fun value -> Interop.set "statusText" value options)
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        Interop.createResponse(!!body, options)

    /// Creates a new response object
    static member create (?body:Blob, ?status:int, ?headers: Map<string, string>, ?statusText:string) : IHttpResponse =
        let options = obj()
        status |> Option.iter (fun value -> Interop.set "status" value options)
        statusText |> Option.iter (fun value -> Interop.set "statusText" value options)
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        Interop.createResponse(!!body, options)

    /// Creates a new response object
    static member create (?body:byte[], ?status:int, ?headers:  Map<string, string>, ?statusText:string) : IHttpResponse =
        let options = obj()
        status |> Option.iter (fun value -> Interop.set "status" value options)
        statusText |> Option.iter (fun value -> Interop.set "statusText" value options)
        headers |> Option.iter (fun value -> Interop.set "headers" (createObj (Array.ofList !!(Map.toList value))) options)
        Interop.createResponse(unbox(Interop.createBlobFromBytes !!body), options)

[<CompiledName "WorkerModule">]
type Worker =
    static member initialize (handler: IHttpRequest -> IHttpResponse) : unit =
        Interop.addEventHandler (fun fetchEvent -> fetchEvent?respondWith(handler fetchEvent?request))

    static member initialize (handler: IHttpRequest -> Async<IHttpResponse>) : unit =
        Interop.addEventHandler (fun fetchEvent -> fetchEvent?respondWith(Async.StartAsPromise(handler fetchEvent?request)))

    static member initialize (handler: IRequestContext -> Async<IHttpResponse>) : unit =
        Interop.addEventHandler <| fun fetchEvent ->
            let requestContext =
                { new IRequestContext with
                    member x.request = fetchEvent?request
                    member x.fetch(request: IHttpRequest) =
                        async {
                            let! response = Async.AwaitPromise(Interop.fetch request)
                            return response
                        }
                }

            fetchEvent?respondWith(Async.StartAsPromise(handler requestContext))